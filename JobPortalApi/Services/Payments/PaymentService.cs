using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using JobPortalApi.DTOs.Payment;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobPortalApi.Services.Payments;

public class PaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public PaymentService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<List<ServicePlanDto>> ListActivePlansAsync()
    {
        var plans = await _context.ServicePlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Entitlements)
            .OrderBy(p => p.Price)
            .ToListAsync();

        return plans.Select(ToPlanDto).ToList();
    }

    public async Task<List<ServicePlanDto>> ListAllPlansAsync()
    {
        var plans = await _context.ServicePlans
            .AsNoTracking()
            .Include(p => p.Entitlements)
            .OrderBy(p => p.Price)
            .ToListAsync();

        return plans.Select(ToPlanDto).ToList();
    }

    public async Task<ServicePlanDto> CreatePlanAsync(CreateServicePlanRequest request)
    {
        ValidateEntitlements(request.Entitlements);
        var plan = new ServicePlan
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            IsActive = request.IsActive,
        };
        foreach (var requestItem in request.Entitlements)
        {
            plan.Entitlements.Add(ToEntitlement(requestItem, plan.Id));
        }

        _context.ServicePlans.Add(plan);
        await _context.SaveChangesAsync();
        return ToPlanDto(plan);
    }

    public async Task<ServicePlanDto?> UpdatePlanAsync(Guid id, UpdateServicePlanRequest request)
    {
        ValidateEntitlements(request.Entitlements);
        var plan = await _context.ServicePlans
            .Include(p => p.Entitlements)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return null;

        plan.Name = request.Name.Trim();
        plan.Price = request.Price;
        plan.Currency = request.Currency.Trim().ToUpperInvariant();
        plan.IsActive = request.IsActive;
        _context.PlanEntitlements.RemoveRange(plan.Entitlements);
        plan.Entitlements.Clear();
        foreach (var requestItem in request.Entitlements)
        {
            _context.PlanEntitlements.Add(ToEntitlement(requestItem, plan.Id));
        }
        await _context.SaveChangesAsync();
        return ToPlanDto(plan);
    }

    public async Task<PaymentOrderDto> CreatePaymentOrderAsync(
        Guid userId,
        CreatePaymentOrderRequest request,
        string ipAddress)
    {
        var plan = await _context.ServicePlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive);
        if (plan == null) throw new KeyNotFoundException("Không tìm thấy gói dịch vụ đang hoạt động.");
        if (!string.Equals(plan.Currency, "VND", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("VNPAY sandbox hiện chỉ hỗ trợ gói tiền tệ VND.");

        var now = DateTime.UtcNow;
        var order = new PaymentOrder
        {
            UserId = userId,
            PlanId = plan.Id,
            VnpTxnRef = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{Random.Shared.Next(100, 999)}",
            Amount = plan.Price,
            Currency = plan.Currency,
            Status = PaymentOrderStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15),
        };

        _context.PaymentOrders.Add(order);
        await _context.SaveChangesAsync();

        return ToPaymentOrderDto(order, BuildPaymentUrl(order, ipAddress));
    }

    public async Task<PaymentOrderDto?> GetPaymentOrderAsync(Guid id, Guid userId, bool isAdmin)
    {
        var order = await _context.PaymentOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null || (!isAdmin && order.UserId != userId)) return null;
        return ToPaymentOrderDto(order);
    }

    public async Task<(string RspCode, string Message)> ProcessVnpayIpnAsync(
        IReadOnlyDictionary<string, string> query)
    {
        var secret = GetRequiredSetting("VNPAY_HASH_SECRET", "Vnpay:HashSecret");
        var parameters = query
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        var secureHash = parameters.GetValueOrDefault("vnp_SecureHash");
        parameters.Remove("vnp_SecureHash");
        parameters.Remove("vnp_SecureHashType");
        if (!VerifyVnpay(parameters, secureHash, secret))
            return ("97", "Invalid signature");

        if (!parameters.TryGetValue("vnp_TxnRef", out var txnRef) ||
            !parameters.TryGetValue("vnp_Amount", out var amountValue) ||
            !decimal.TryParse(amountValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawAmount))
            return ("04", "Invalid payment data");

        var order = await _context.PaymentOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.VnpTxnRef == txnRef);
        if (order == null) return ("01", "Order not found");

        var amount = rawAmount / 100m;
        if (amount != order.Amount) return ("04", "Invalid amount");
        if (order.Status == PaymentOrderStatus.Paid) return ("00", "Confirm Success");
        if (order.Status != PaymentOrderStatus.Pending) return ("02", "Order already processed");

        var responseCode = parameters.GetValueOrDefault("vnp_ResponseCode") ?? "99";
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var current = await _context.PaymentOrders
            .Include(o => o.Plan)
            .ThenInclude(p => p.Entitlements)
            .FirstOrDefaultAsync(o => o.Id == order.Id);
        if (current == null)
            return ("01", "Order not found");
        if (current.Status == PaymentOrderStatus.Paid)
        {
            await transaction.CommitAsync();
            return ("00", "Confirm Success");
        }

        if (responseCode == "00" && current.ExpiresAt > DateTime.UtcNow)
        {
            current.Status = PaymentOrderStatus.Paid;
            current.PaidAt = DateTime.UtcNow;
            current.ProviderResponseCode = responseCode;
            foreach (var entitlement in current.Plan.Entitlements)
            {
                _context.CreditLedgers.Add(new CreditLedger
                {
                    UserId = current.UserId,
                    PaymentOrderId = current.Id,
                    CreditType = entitlement.CreditType,
                    Quantity = entitlement.Quantity,
                    ExpiresAt = entitlement.ExpiresInDays.HasValue
                        ? DateTime.UtcNow.AddDays(entitlement.ExpiresInDays.Value)
                        : null,
                });
            }
        }
        else
        {
            current.Status = current.ExpiresAt <= DateTime.UtcNow
                ? PaymentOrderStatus.Expired
                : PaymentOrderStatus.Failed;
            current.ProviderResponseCode = responseCode;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ("00", "Confirm Success");
    }

    public async Task<CreditBalanceDto> GetBalanceAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var balances = await _context.CreditLedgers
            .AsNoTracking()
            .Where(entry => entry.UserId == userId &&
                (!entry.ExpiresAt.HasValue || entry.ExpiresAt > now))
            .GroupBy(entry => entry.CreditType)
            .Select(group => new { Type = group.Key, Quantity = group.Sum(entry => entry.Quantity) })
            .ToDictionaryAsync(item => item.Type, item => item.Quantity);
        return new CreditBalanceDto { Balances = balances };
    }

    public async Task<List<CreditLedgerDto>> GetLedgerAsync(Guid userId)
    {
        return await _context.CreditLedgers
            .AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new CreditLedgerDto
            {
                Id = entry.Id,
                CreditType = entry.CreditType,
                Quantity = entry.Quantity,
                ExpiresAt = entry.ExpiresAt,
                CreatedAt = entry.CreatedAt,
                PaymentOrderId = entry.PaymentOrderId,
            })
            .ToListAsync();
    }

    public static string BuildVnpaySignData(IReadOnlyDictionary<string, string> parameters)
    {
        return string.Join("&", parameters
            .Where(item => !string.IsNullOrEmpty(item.Value))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"{Encode(item.Key)}={Encode(item.Value)}"));
    }

    public static string SignVnpay(IReadOnlyDictionary<string, string> parameters, string secret)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(BuildVnpaySignData(parameters)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool VerifyVnpay(
        IReadOnlyDictionary<string, string> parameters,
        string? secureHash,
        string secret)
    {
        if (string.IsNullOrWhiteSpace(secureHash)) return false;
        var expected = SignVnpay(parameters, secret);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(secureHash.ToLowerInvariant());
        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private string BuildPaymentUrl(PaymentOrder order, string ipAddress)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = GetRequiredSetting("VNPAY_TMN_CODE", "Vnpay:TmnCode"),
            ["vnp_Amount"] = ((long)Math.Round(order.Amount * 100m, 0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = order.VnpTxnRef,
            ["vnp_OrderInfo"] = $"Thanh toan goi dich vu {order.VnpTxnRef}",
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = GetSetting("VNPAY_RETURN_URL", "Vnpay:ReturnUrl") ?? "http://localhost:3000/vi/payment/return",
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
            ["vnp_CreateDate"] = FormatVnpayDate(order.CreatedAt),
            ["vnp_ExpireDate"] = FormatVnpayDate(order.ExpiresAt),
        };
        var signature = SignVnpay(parameters, GetRequiredSetting("VNPAY_HASH_SECRET", "Vnpay:HashSecret"));
        var query = string.Join("&", parameters.OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"{Encode(item.Key)}={Encode(item.Value)}"));
        var paymentUrl = GetSetting("VNPAY_PAYMENT_URL", "Vnpay:PaymentUrl")
            ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        return $"{paymentUrl}?{query}&vnp_SecureHash={signature}";
    }

    private static string FormatVnpayDate(DateTime date)
    {
        var local = new DateTimeOffset(date, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(7));
        return local.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
    }

    private static string Encode(string value) => Uri.EscapeDataString(value).Replace("%20", "+");

    private static ServicePlanDto ToPlanDto(ServicePlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Price = plan.Price,
        Currency = plan.Currency,
        IsActive = plan.IsActive,
        Entitlements = plan.Entitlements.Select(item => new PlanEntitlementDto
        {
            CreditType = item.CreditType,
            Quantity = item.Quantity,
            ExpiresInDays = item.ExpiresInDays,
        }).ToList(),
    };

    private static PaymentOrderDto ToPaymentOrderDto(PaymentOrder order, string? paymentUrl = null) => new()
    {
        Id = order.Id,
        PlanId = order.PlanId,
        VnpTxnRef = order.VnpTxnRef,
        Amount = order.Amount,
        Currency = order.Currency,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        ExpiresAt = order.ExpiresAt,
        PaidAt = order.PaidAt,
        PaymentUrl = paymentUrl,
    };

    private static PlanEntitlement ToEntitlement(PlanEntitlementRequest request, Guid planId) => new()
    {
        PlanId = planId,
        CreditType = request.CreditType,
        Quantity = request.Quantity,
        ExpiresInDays = request.ExpiresInDays,
    };

    private static void ValidateEntitlements(IEnumerable<PlanEntitlementRequest> entitlements)
    {
        var items = entitlements.ToList();
        if (items.Count == 0 || items.Select(item => item.CreditType).Distinct().Count() != items.Count)
            throw new ArgumentException("Mỗi loại tín dụng chỉ được khai báo một lần trong gói.");
    }

    private string GetRequiredSetting(string environmentKey, string configurationKey)
    {
        return GetSetting(environmentKey, configurationKey)
            ?? throw new InvalidOperationException($"Thiếu cấu hình {environmentKey}.");
    }

    private string? GetSetting(string environmentKey, string configurationKey)
    {
        return Environment.GetEnvironmentVariable(environmentKey) ?? _configuration[configurationKey];
    }
}

using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JobPortalApi.DTOs.Payment;
using JobPortalApi.Models;
using JobPortalApi.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortalApi.Services.Payments;

public class PaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    private sealed record EntitlementSnapshot(CreditType CreditType, int Quantity, int? ExpiresInDays);

    public PaymentService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
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
            .Include(p => p.Entitlements)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive);
        if (plan == null) throw new KeyNotFoundException("Không tìm thấy gói dịch vụ đang hoạt động.");
        if (!string.Equals(plan.Currency, "VND", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("VNPAY sandbox hiện chỉ hỗ trợ gói tiền tệ VND.");
        if (plan.Price <= 0)
            throw new InvalidOperationException("Gói dịch vụ phải có giá lớn hơn 0.");

        var now = DateTime.UtcNow;
        var order = new PaymentOrder
        {
            UserId = userId,
            PlanId = plan.Id,
            VnpTxnRef = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{Random.Shared.Next(100, 999)}",
            Amount = plan.Price,
            PlanNameSnapshot = plan.Name,
            PriceSnapshot = plan.Price,
            EntitlementsSnapshot = JsonSerializer.Serialize(plan.Entitlements.Select(item =>
                new EntitlementSnapshot(item.CreditType, item.Quantity, item.ExpiresInDays))),
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

    public async Task<PaymentOrderDto?> GetPaymentOrderByTxnRefAsync(
        string txnRef,
        Guid userId,
        bool isAdmin)
    {
        var normalizedTxnRef = txnRef.Trim();
        if (normalizedTxnRef.Length == 0 || normalizedTxnRef.Length > 64) return null;

        var order = await _context.PaymentOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.VnpTxnRef == normalizedTxnRef);
        if (order == null || (!isAdmin && order.UserId != userId)) return null;
        return ToPaymentOrderDto(order);
    }

    public async Task<List<PaymentOrderListItemDto>> ListPaymentOrdersAsync(Guid? userId = null)
    {
        var query = _context.PaymentOrders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Plan)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new PaymentOrderListItemDto
            {
                Id = o.Id,
                UserId = o.UserId,
                UserFullName = o.User != null ? (o.User.FullName ?? string.Empty) : string.Empty,
                UserEmail = o.User != null ? (o.User.Email ?? string.Empty) : string.Empty,
                PlanId = o.PlanId,
                PlanName = o.Plan != null ? o.Plan.Name : string.Empty,
                VnpTxnRef = o.VnpTxnRef,
                VnpTransactionNo = o.VnpTransactionNo,
                Amount = o.Amount,
                Currency = o.Currency,
                Status = o.Status,
                ProviderResponseCode = o.ProviderResponseCode,
                CreatedAt = o.CreatedAt,
                PaidAt = o.PaidAt,
                ExpiresAt = o.ExpiresAt,
            })
            .ToListAsync();
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

        var requiredFields = new[]
        {
            "vnp_TmnCode",
            "vnp_CurrCode",
            "vnp_Amount",
            "vnp_TxnRef",
            "vnp_ResponseCode",
            "vnp_TransactionStatus",
            "vnp_TransactionNo",
        };
        if (requiredFields.Any(field => !parameters.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value)))
            return ("04", "Invalid payment data");

        var transactionNo = parameters["vnp_TransactionNo"].Trim();
        if (!IsValidVnpayTransactionNo(transactionNo))
            return ("04", "Invalid transaction number");

        var expectedTmnCode = GetRequiredSetting("VNPAY_TMN_CODE", "Vnpay:TmnCode");
        if (!string.Equals(parameters["vnp_TmnCode"], expectedTmnCode, StringComparison.Ordinal))
            return ("04", "Invalid merchant");
        if (!string.Equals(parameters["vnp_CurrCode"], "VND", StringComparison.OrdinalIgnoreCase))
            return ("04", "Invalid currency");
        if (!decimal.TryParse(parameters["vnp_Amount"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawAmount) ||
            rawAmount <= 0)
            return ("04", "Invalid amount");

        var txnRef = parameters["vnp_TxnRef"].Trim();
        var responseCode = parameters["vnp_ResponseCode"];
        var transactionStatus = parameters["vnp_TransactionStatus"];
        var order = await _context.PaymentOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.VnpTxnRef == txnRef);
        if (order == null) return ("01", "Order not found");

        var amount = rawAmount / 100m;
        var expectedAmount = order.PriceSnapshot ?? order.Amount;
        if (amount != expectedAmount) return ("04", "Invalid amount");

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var current = await _context.PaymentOrders
                    .FirstOrDefaultAsync(o => o.Id == order.Id);
                if (current == null)
                    return ("01", "Order not found");

                var transactionOwner = await _context.PaymentOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.VnpTransactionNo == transactionNo);
                if (transactionOwner != null && transactionOwner.Id != current.Id)
                {
                    _logger.LogWarning(
                        "VNPAY transaction number is already bound to another order. PaymentOrderId={PaymentOrderId}, TxnRef={TxnRef}",
                        current.Id,
                        current.VnpTxnRef);
                    return ("04", "Transaction already bound");
                }

                if (current.Status == PaymentOrderStatus.Paid)
                {
                    if (string.Equals(current.VnpTransactionNo, transactionNo, StringComparison.Ordinal))
                    {
                        await transaction.CommitAsync();
                        return ("00", "Confirm Success");
                    }

                    _logger.LogWarning(
                        "VNPAY transaction binding mismatch for paid order. PaymentOrderId={PaymentOrderId}, TxnRef={TxnRef}",
                        current.Id,
                        current.VnpTxnRef);
                    return ("04", "Transaction does not match order");
                }

                if (current.Status != PaymentOrderStatus.Pending)
                    return ("02", "Order already processed");

                var now = DateTime.UtcNow;
                if (responseCode == "00" && transactionStatus == "00" && current.ExpiresAt > now)
                {
                    List<EntitlementSnapshot>? snapshots;
                    try
                    {
                        snapshots = string.IsNullOrWhiteSpace(current.EntitlementsSnapshot)
                            ? null
                            : JsonSerializer.Deserialize<List<EntitlementSnapshot>>(current.EntitlementsSnapshot);
                    }
                    catch (JsonException)
                    {
                        snapshots = null;
                    }

                    if (snapshots is not { Count: > 0 } ||
                        snapshots.Any(item => item.Quantity <= 0 || (item.ExpiresInDays.HasValue && item.ExpiresInDays <= 0)) ||
                        snapshots.Select(item => item.CreditType).Distinct().Count() != snapshots.Count)
                    {
                        current.Status = PaymentOrderStatus.Failed;
                        current.ProviderResponseCode = "98";
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return ("04", "Order entitlement snapshot is invalid");
                    }

                    current.Status = PaymentOrderStatus.Paid;
                    current.PaidAt = now;
                    current.ProviderResponseCode = responseCode;
                    current.VnpTransactionNo = transactionNo;
                    foreach (var entitlement in snapshots)
                    {
                        _context.CreditLedgers.Add(new CreditLedger
                        {
                            UserId = current.UserId,
                            PaymentOrderId = current.Id,
                            CreditType = entitlement.CreditType,
                            EntryType = CreditLedgerEntryType.Grant,
                            IdempotencyKey = $"payment:{current.Id}:{entitlement.CreditType}",
                            Quantity = entitlement.Quantity,
                            ExpiresAt = entitlement.ExpiresInDays.HasValue
                                ? now.AddDays(entitlement.ExpiresInDays.Value)
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
            });
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _logger.LogWarning(
                exception,
                "VNPAY transaction number could not be bound because it is already in use. TxnRef={TxnRef}",
                txnRef);
            return ("04", "Transaction already bound");
        }
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
                EntryType = entry.EntryType,
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
        var paymentUrl = GetRequiredSetting("VNPAY_PAYMENT_URL", "Vnpay:PaymentUrl");
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
        VnpTransactionNo = order.VnpTransactionNo,
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

    private static bool IsValidVnpayTransactionNo(string value) =>
        value.Length is > 0 and <= 64 && value.All(character => character is >= '0' and <= '9');

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException &&
        (sqlException.Number == 2601 || sqlException.Number == 2627);

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

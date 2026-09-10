using System.Security.Claims;
using JobPortalApi.DTOs.Payment;
using JobPortalApi.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobPortalApi.Controllers.Payments;

[ApiController]
[EnableRateLimiting("payment")]
[Route("api")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly CreditLedgerService _creditLedgerService;

    public PaymentController(PaymentService paymentService, CreditLedgerService creditLedgerService)
    {
        _paymentService = paymentService;
        _creditLedgerService = creditLedgerService;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        return Ok(await _paymentService.ListActivePlansAsync());
    }

    [HttpGet("admin/plans")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPlans()
    {
        return Ok(await _paymentService.ListAllPlansAsync());
    }

    [HttpPost("admin/plans")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateServicePlanRequest request)
    {
        try
        {
            return Ok(await _paymentService.CreatePlanAsync(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("admin/plans/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateServicePlanRequest request)
    {
        try
        {
            var plan = await _paymentService.UpdatePlanAsync(id, request);
            return plan == null ? NotFound() : Ok(plan);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payment-orders")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> CreatePaymentOrder([FromBody] CreatePaymentOrderRequest request)
    {
        try
        {
            var userId = GetUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            return Ok(await _paymentService.CreatePaymentOrderAsync(userId, request, ipAddress));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("payment-orders/{id:guid}")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> GetPaymentOrder(Guid id)
    {
        var result = await _paymentService.GetPaymentOrderAsync(id, GetUserId(), User.IsInRole("Admin"));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("payment-orders/by-txn-ref/{txnRef}")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> GetPaymentOrderByTxnRef(string txnRef)
    {
        var result = await _paymentService.GetPaymentOrderByTxnRefAsync(
            txnRef,
            GetUserId(),
            User.IsInRole("Admin"));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("admin/payment-orders")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPaymentOrders()
    {
        return Ok(await _paymentService.ListPaymentOrdersAsync());
    }

    [HttpGet("recruiter/payment-orders")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> GetMyPaymentOrders()
    {
        return Ok(await _paymentService.ListPaymentOrdersAsync(GetUserId()));
    }

    [HttpGet("payments/vnpay/return")]
    [AllowAnonymous]
    public IActionResult VnpayReturn([FromQuery] string? vnp_ResponseCode, [FromQuery] string? vnp_TxnRef)
    {
        return Ok(new { responseCode = vnp_ResponseCode, txnRef = vnp_TxnRef });
    }

    [HttpGet("payments/vnpay/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayIpn()
    {
        var query = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
        var result = await _paymentService.ProcessVnpayIpnAsync(query);
        return Ok(new { RspCode = result.RspCode, Message = result.Message });
    }

    [HttpGet("credits/balance")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> GetBalance()
    {
        return Ok(await _paymentService.GetBalanceAsync(GetUserId()));
    }

    [HttpGet("credits/ledger")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> GetLedger()
    {
        return Ok(await _paymentService.GetLedgerAsync(GetUserId()));
    }

    [HttpPost("admin/credits/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RefundCredit([FromBody] CreditRefundRequest request)
    {
        try
        {
            var refunded = await _creditLedgerService.TryRefundAsync(
                request.UserId,
                request.CreditType,
                request.Quantity,
                request.IdempotencyKey,
                request.SourceIdempotencyKey,
                actorId: GetUserId(),
                reason: request.Reason);
            return refunded
                ? Ok(new { success = true })
                : Conflict(new { message = "Debit không tồn tại, đã refund hoặc dữ liệu refund không hợp lệ." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("admin/credits/adjustment")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdjustCredit([FromBody] CreditAdjustmentRequest request)
    {
        try
        {
            var adjusted = await _creditLedgerService.TryAdjustAsync(
                request.UserId,
                request.CreditType,
                request.Quantity,
                request.IdempotencyKey,
                GetUserId(),
                request.Reason);
            return adjusted
                ? Ok(new { success = true })
                : Conflict(new { message = "Adjustment bị từ chối vì làm số dư âm hoặc idempotency payload không khớp." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
            throw new InvalidOperationException("Phiên đăng nhập không hợp lệ.");
        return userId;
    }
}

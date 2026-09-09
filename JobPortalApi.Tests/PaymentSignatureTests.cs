using JobPortalApi.Services.Payments;
using Xunit;

namespace JobPortalApi.Tests;

public class PaymentSignatureTests
{
    [Fact]
    public void BuildVnpaySignData_SortsAndEncodesParameters()
    {
        var parameters = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_Command"] = "pay",
            ["vnp_OrderInfo"] = "Thanh toán gói cơ bản",
            ["vnp_TxnRef"] = "order-123",
        };

        var result = PaymentService.BuildVnpaySignData(parameters);

        Assert.Equal(
            "vnp_Amount=1000000&vnp_Command=pay&vnp_OrderInfo=Thanh+to%C3%A1n+g%C3%B3i+c%C6%A1+b%E1%BA%A3n&vnp_TxnRef=order-123",
            result);
    }

    [Fact]
    public void VerifyVnpay_RejectsTamperedPayload()
    {
        var secret = "sandbox-secret";
        var parameters = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_Command"] = "pay",
            ["vnp_TxnRef"] = "order-123",
        };
        var signature = PaymentService.SignVnpay(parameters, secret);

        Assert.True(PaymentService.VerifyVnpay(parameters, signature, secret));
        Assert.False(PaymentService.VerifyVnpay(
            new Dictionary<string, string>(parameters) { ["vnp_Amount"] = "2000000" },
            signature,
            secret));
    }
}

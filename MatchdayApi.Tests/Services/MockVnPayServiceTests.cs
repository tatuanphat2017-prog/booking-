using MatchdayApi.Models;
using MatchdayApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace MatchdayApi.Tests.Services;

public class MockVnPayServiceTests
{
    private static Booking BuildBooking() => new()
    {
        Id = 1,
        BookingCode = "BK260101120000",
        TotalAmount = 150000
    };

    [Fact]
    public void CreatePaymentUrl_ShouldBuildUrl_PointingToMockPage_WithCorrectAmount()
    {
        var service = new MockVnPayService();
        var booking = BuildBooking();
        const string returnUrl = "https://localhost:5001/api/Payments/vnpay-return";

        var (txnRef, paymentUrl) = service.CreatePaymentUrl(booking, "127.0.0.1", returnUrl);

        Assert.StartsWith("BK260101120000", txnRef);
        Assert.StartsWith("https://localhost:5001/vnpay-mock.html?", paymentUrl);
        Assert.Contains("amount=150000", paymentUrl);
        Assert.Contains($"txnRef={Uri.EscapeDataString(txnRef)}", paymentUrl);
    }

    [Fact]
    public void ValidateResponse_ShouldReturnSuccess_WhenResponseCodeIs00()
    {
        var service = new MockVnPayService();
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["vnp_ResponseCode"] = "00",
            ["vnp_TransactionStatus"] = "00",
            ["vnp_TxnRef"] = "BK260101120000",
            ["vnp_Amount"] = "150000"
        });

        var result = service.ValidateResponse(query);

        Assert.True(result.IsValidSignature);
        Assert.True(result.IsSuccess);
        Assert.Equal("BK260101120000", result.TxnRef);
        Assert.Equal(150000, result.Amount);
    }

    [Fact]
    public void ValidateResponse_ShouldReturnFailure_WhenResponseCodeIsNot00()
    {
        var service = new MockVnPayService();
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["vnp_ResponseCode"] = "24",
            ["vnp_TransactionStatus"] = "02",
            ["vnp_TxnRef"] = "BK260101120000",
            ["vnp_Amount"] = "150000"
        });

        var result = service.ValidateResponse(query);

        Assert.False(result.IsSuccess);
        Assert.Equal("24", result.ResponseCode);
    }
}

using MatchdayApi.Models;
using Microsoft.AspNetCore.Http;

namespace MatchdayApi.Services;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _config;

    public VnPayService(IConfiguration config)
    {
        _config = config;
    }

    public (string TxnRef, string PaymentUrl) CreatePaymentUrl(Booking booking, string ipAddress, string returnUrl)
    {
        var section = _config.GetSection("VnPay");
        var tmnCode = section["TmnCode"];
        var hashSecret = section["HashSecret"];
        var baseUrl = section["BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

        if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret))
        {
            throw new InvalidOperationException(
                "Thiếu cấu hình VnPay:TmnCode / VnPay:HashSecret. Đặt qua User Secrets, không hardcode vào appsettings.");
        }

        // Giờ Việt Nam (UTC+7) — VNPay yêu cầu vnp_CreateDate/vnp_ExpireDate theo giờ địa phương của merchant.
        var now = DateTime.UtcNow.AddHours(7);
        var txnRef = $"{booking.BookingCode}{now:HHmmss}";

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", tmnCode);
        vnpay.AddRequestData("vnp_Amount", ((long)(booking.TotalAmount * 100)).ToString());
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_TxnRef", txnRef);
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don {booking.BookingCode}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_IpAddr", ipAddress);
        vnpay.AddRequestData("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

        var url = vnpay.CreateRequestUrl(baseUrl, hashSecret);
        return (txnRef, url);
    }

    public VnPayResponseResult ValidateResponse(IQueryCollection query)
    {
        var hashSecret = _config["VnPay:HashSecret"] ?? string.Empty;
        var vnpay = new VnPayLibrary();
        var inputHash = string.Empty;

        foreach (var kv in query)
        {
            if (!kv.Key.StartsWith("vnp_")) continue;

            if (kv.Key == "vnp_SecureHash")
            {
                inputHash = kv.Value.ToString();
            }
            else
            {
                vnpay.AddResponseData(kv.Key, kv.Value.ToString());
            }
        }

        var isValidSignature = !string.IsNullOrEmpty(inputHash) && vnpay.ValidateSignature(inputHash, hashSecret);
        var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var transactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");

        return new VnPayResponseResult
        {
            IsValidSignature = isValidSignature,
            IsSuccess = isValidSignature && responseCode == "00" && transactionStatus == "00",
            TxnRef = vnpay.GetResponseData("vnp_TxnRef"),
            ResponseCode = responseCode,
            TransactionStatus = transactionStatus,
            Amount = long.TryParse(vnpay.GetResponseData("vnp_Amount"), out var amount) ? amount / 100 : 0,
            OrderInfo = vnpay.GetResponseData("vnp_OrderInfo"),
            TransactionNo = vnpay.GetResponseData("vnp_TransactionNo"),
            BankCode = vnpay.GetResponseData("vnp_BankCode"),
            PayDate = vnpay.GetResponseData("vnp_PayDate")
        };
    }
}

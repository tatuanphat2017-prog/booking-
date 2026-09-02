using MatchdayApi.Models;
using Microsoft.AspNetCore.Http;

namespace MatchdayApi.Services;

public interface IVnPayService
{
    /// <summary>Sinh URL redirect người dùng sang trang thanh toán VNPay sandbox.
    /// Trả kèm vnp_TxnRef đã dùng để controller lưu lại, đối chiếu lúc VNPay callback.</summary>
    (string TxnRef, string PaymentUrl) CreatePaymentUrl(Booking booking, string ipAddress, string returnUrl);

    /// <summary>Xác thực chữ ký + đọc dữ liệu từ query string VNPay gửi về (Return URL hoặc IPN).</summary>
    VnPayResponseResult ValidateResponse(IQueryCollection query);
}

public class VnPayResponseResult
{
    public bool IsValidSignature { get; set; }
    public bool IsSuccess { get; set; }
    public string TxnRef { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;

    /// <summary>Số tiền — đã chia lại /100 so với vnp_Amount gốc (VNPay yêu cầu gửi số tiền x100).</summary>
    public long Amount { get; set; }

    public string OrderInfo { get; set; } = string.Empty;
    public string TransactionNo { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string PayDate { get; set; } = string.Empty;
}

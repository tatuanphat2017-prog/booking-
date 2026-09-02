using MatchdayApi.Models;
using Microsoft.AspNetCore.Http;

namespace MatchdayApi.Services;

/// <summary>
/// Phiên bản GIẢ LẬP (mock) của VNPay — dùng khi chưa có tài khoản VNPay sandbox thật (Mã website /
/// Chuỗi bí mật), hoặc chỉ muốn test nhanh luồng đặt vé -> thanh toán -> vé mà không cần mạng/tài khoản
/// bên thứ ba. Không gọi ra ngoài internet, không cần TmnCode/HashSecret.
///
/// Thay vì redirect sang https://sandbox.vnpayment.vn/..., CreatePaymentUrl trả về link tới trang
/// wwwroot/vnpay-mock.html (tự host ngay trong project này) — trang đó có 2 nút "Giả lập thành công" /
/// "Giả lập thất bại" để tự tạo ra kết quả redirect y hệt format VNPay thật gửi về (vnp_ResponseCode,
/// vnp_TransactionStatus...), nhờ vậy PaymentsController.VnPayReturn/VnPayIpn xử lý y hệt như khi dùng
/// VNPay thật, không cần sửa code Controller.
///
/// Muốn chuyển sang dùng VNPay sandbox thật: đổi cấu hình "VnPay:Mode" thành "Real" (kèm TmnCode/HashSecret
/// thật qua dotnet user-secrets) — Program.cs sẽ tự chuyển sang dùng VnPayService thật, không cần đổi gì thêm.
/// </summary>
public class MockVnPayService : IVnPayService
{
    public (string TxnRef, string PaymentUrl) CreatePaymentUrl(Booking booking, string ipAddress, string returnUrl)
    {
        var now = DateTime.UtcNow.AddHours(7); // giờ VN, giống quy ước của VnPayService thật
        var txnRef = $"{booking.BookingCode}{now:HHmmss}";
        var amount = (long)booking.TotalAmount;
        var orderInfo = $"Thanh toan don {booking.BookingCode}";

        var query = $"?txnRef={Uri.EscapeDataString(txnRef)}" +
                    $"&amount={amount}" +
                    $"&orderInfo={Uri.EscapeDataString(orderInfo)}" +
                    $"&returnUrl={Uri.EscapeDataString(returnUrl)}";

        // Ghép cùng scheme+host với returnUrl để ra URL tuyệt đối, mở được trực tiếp trên trình duyệt.
        var baseUri = new Uri(returnUrl);
        var mockPageUrl = $"{baseUri.Scheme}://{baseUri.Authority}/vnpay-mock.html{query}";

        return (txnRef, mockPageUrl);
    }

    public VnPayResponseResult ValidateResponse(IQueryCollection query)
    {
        // vnpay-mock.html tự gắn các tham số vnp_* (đúng tên) vào Return URL khi bấm nút giả lập,
        // nên chỉ cần đọc trực tiếp — không có chữ ký HMAC thật để xác thực trong chế độ mock.
        var responseCode = query["vnp_ResponseCode"].ToString();
        var transactionStatus = query["vnp_TransactionStatus"].ToString();

        return new VnPayResponseResult
        {
            IsValidSignature = true, // mock: luôn coi là hợp lệ vì không có VNPay thật ký dữ liệu
            IsSuccess = responseCode == "00" && transactionStatus == "00",
            TxnRef = query["vnp_TxnRef"].ToString(),
            ResponseCode = responseCode,
            TransactionStatus = transactionStatus,
            Amount = long.TryParse(query["vnp_Amount"].ToString(), out var amount) ? amount : 0,
            OrderInfo = query["vnp_OrderInfo"].ToString(),
            TransactionNo = $"MOCK{DateTime.UtcNow.Ticks}",
            BankCode = "MOCKBANK",
            PayDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss")
        };
    }
}

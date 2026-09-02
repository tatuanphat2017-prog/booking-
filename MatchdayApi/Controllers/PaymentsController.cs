using System.Security.Claims;
using MatchdayApi.Data;
using MatchdayApi.Enums;
using MatchdayApi.Models;
using MatchdayApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

/// <summary>
/// Tích hợp thanh toán VNPay sandbox (task #13). Luồng hoạt động:
/// 1. User đã có Booking (status Pending, từ task #12) -> gọi CreatePaymentUrl để lấy link VNPay.
/// 2. Frontend redirect trình duyệt user sang link đó -> user thao tác trên trang VNPay sandbox.
/// 3. VNPay redirect trình duyệt user quay lại VnPayReturn (kèm kết quả) -> hiện trang kết quả.
/// 4. Song song đó, VNPay server gọi VnPayIpn (server-to-server) để xác nhận kết quả đáng tin cậy hơn
///    (không phụ thuộc trình duyệt user có quay lại Return URL hay không). Khi deploy thật (task #26),
///    URL IPN phải là domain public thì VNPay mới gọi vào được — chạy local thì chỉ Return URL hoạt động.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IVnPayService _vnPayService;
    private readonly ITicketService _ticketService;
    private readonly IEmailService _emailService;

    public PaymentsController(AppDbContext db, IVnPayService vnPayService, ITicketService ticketService, IEmailService emailService)
    {
        _db = db;
        _vnPayService = vnPayService;
        _ticketService = ticketService;
        _emailService = emailService;
    }

    /// <summary>
    /// Task #14: khi thanh toán chuyển Paid — sinh vé QR cho từng ghế + gửi (hoặc giả lập gửi) email
    /// xác nhận. Gọi chung từ cả VnPayReturn lẫn VnPayIpn (idempotent nhờ ITicketService không tạo vé
    /// trùng). Bọc try/catch để nếu sinh vé/gửi mail lỗi thì cũng không làm hỏng luồng xác nhận thanh toán.
    /// </summary>
    private async Task GenerateTicketsAndSendEmailAsync(int bookingId)
    {
        try
        {
            await _ticketService.GenerateTicketsForBookingAsync(bookingId);
            await _emailService.SendBookingConfirmationAsync(bookingId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentsController] Lỗi khi sinh vé/gửi email cho bookingId={bookingId}: {ex.Message}");
        }
    }

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Sinh URL thanh toán VNPay cho 1 đơn đặt vé đang Pending — chỉ chủ đơn mới gọi được.</summary>
    [Authorize]
    [HttpPost("vnpay/create/{bookingId:int}")]
    public async Task<IActionResult> CreatePaymentUrl(int bookingId)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đặt vé." });

        if (booking.UserId != CurrentUserId)
        {
            return Forbid();
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return BadRequest(new { message = "Đơn đặt vé này không ở trạng thái chờ thanh toán." });
        }

        string paymentUrl;
        string txnRef;
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
            var returnUrl = $"{Request.Scheme}://{Request.Host}/api/Payments/vnpay-return";
            (txnRef, paymentUrl) = _vnPayService.CreatePaymentUrl(booking, ip, returnUrl);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }

        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            Method = "VNPay",
            Status = PaymentStatus.Pending,
            VnpTransactionId = txnRef
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return Ok(new { paymentUrl });
    }

    /// <summary>VNPay redirect trình duyệt user về đây sau khi thanh toán xong (thành công hoặc hủy/thất bại).</summary>
    [HttpGet("vnpay-return")]
    public async Task<ContentResult> VnPayReturn()
    {
        var result = _vnPayService.ValidateResponse(Request.Query);

        var payment = await _db.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.VnpTransactionId == result.TxnRef);

        string title;
        string message;
        var success = false;

        if (payment is null)
        {
            title = "Không tìm thấy giao dịch";
            message = "Không tìm thấy đơn thanh toán tương ứng với mã giao dịch này.";
        }
        else if (!result.IsValidSignature)
        {
            title = "Dữ liệu không hợp lệ";
            message = "Chữ ký từ VNPay không khớp — có thể dữ liệu đã bị thay đổi. Vui lòng thử lại.";
        }
        else if (result.IsSuccess)
        {
            if (payment.Status != PaymentStatus.Success)
            {
                payment.Status = PaymentStatus.Success;
                payment.PaidAt = DateTime.UtcNow;
                payment.Booking.Status = BookingStatus.Paid;
                await _db.SaveChangesAsync();

                await GenerateTicketsAndSendEmailAsync(payment.BookingId);
            }

            success = true;
            title = "Thanh toán thành công!";
            message = $"Mã đơn: {payment.Booking.BookingCode} — Số tiền: {payment.Amount:N0}đ";
        }
        else
        {
            if (payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Failed;
                await _db.SaveChangesAsync();
            }

            title = "Thanh toán không thành công";
            message = $"Mã đơn: {payment.Booking.BookingCode} — Mã lỗi VNPay: {result.ResponseCode}";
        }

        return Content(BuildResultHtml(title, message, success), "text/html");
    }

    /// <summary>
    /// VNPay server gọi thẳng vào đây (không qua trình duyệt user) để xác nhận kết quả — đáng tin cậy hơn
    /// Return URL. Phải trả đúng format JSON RspCode/Message theo tài liệu VNPay, không được đổi khác.
    /// </summary>
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn()
    {
        var result = _vnPayService.ValidateResponse(Request.Query);

        if (!result.IsValidSignature)
        {
            return Ok(new { RspCode = "97", Message = "Invalid signature" });
        }

        var payment = await _db.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.VnpTransactionId == result.TxnRef);

        if (payment is null)
        {
            return Ok(new { RspCode = "01", Message = "Order not found" });
        }

        if ((long)payment.Amount != result.Amount)
        {
            return Ok(new { RspCode = "04", Message = "Invalid amount" });
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return Ok(new { RspCode = "02", Message = "Order already confirmed" });
        }

        payment.Status = result.IsSuccess ? PaymentStatus.Success : PaymentStatus.Failed;
        if (result.IsSuccess)
        {
            payment.PaidAt = DateTime.UtcNow;
            payment.Booking.Status = BookingStatus.Paid;
        }

        await _db.SaveChangesAsync();

        if (result.IsSuccess)
        {
            await GenerateTicketsAndSendEmailAsync(payment.BookingId);
        }

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    private static string BuildResultHtml(string title, string message, bool success)
    {
        var color = success ? "#2ECC71" : "#E74C3C";
        var sb = new System.Text.StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"UTF-8\" />");
        sb.Append("<title>").Append(title).Append("</title>");
        sb.Append("<style>");
        sb.Append("body{font-family:Segoe UI,Arial,sans-serif;background:#0A0C10;color:#F4F6FA;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;}");
        sb.Append(".card{background:#12151b;border:1px solid #23272f;border-radius:12px;padding:32px 40px;text-align:center;max-width:440px;}");
        sb.Append("h1{color:").Append(color).Append(";font-size:22px;margin-bottom:12px;}");
        sb.Append("p{color:#9aa4b2;font-size:14px;}");
        sb.Append("</style></head>");
        sb.Append("<body><div class=\"card\"><h1>").Append(title).Append("</h1><p>").Append(message).Append("</p></div></body></html>");
        return sb.ToString();
    }
}

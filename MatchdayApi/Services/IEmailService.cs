namespace MatchdayApi.Services;

/// <summary>
/// Gửi email xác nhận đặt vé kèm vé QR (task #14). Có 2 bản triển khai, chọn qua config "Email:Mode":
/// - MockEmailService (mặc định, "Mock"): không gửi email thật, chỉ lưu lại thành file HTML trong
///   wwwroot/sent-emails/ để mở xem trực tiếp trên trình duyệt — không cần tài khoản email/SMTP thật,
///   tránh lặp lại rắc rối như lúc tìm tài khoản VNPay.
/// - SmtpEmailService ("Real"): gửi email thật qua SMTP (vd Gmail + App Password).
///
/// Chỉ cần truyền bookingId — service tự tải Booking/User/Match/Tickets cần thiết từ DB.
/// </summary>
public interface IEmailService
{
    Task SendBookingConfirmationAsync(int bookingId);
}

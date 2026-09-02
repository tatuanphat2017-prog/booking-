using System.Net;
using System.Net.Mail;
using MatchdayApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Services;

/// <summary>
/// Gửi email THẬT qua SMTP (vd Gmail). Chỉ dùng khi "Email:Mode" = "Real" và đã set đủ
/// Email:Username / Email:Password (App Password, KHÔNG phải mật khẩu Gmail thường) qua User Secrets.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly AppDbContext _db;
    private readonly IQrCodeService _qr;
    private readonly IConfiguration _config;

    public SmtpEmailService(AppDbContext db, IQrCodeService qr, IConfiguration config)
    {
        _db = db;
        _qr = qr;
        _config = config;
    }

    public async Task SendBookingConfirmationAsync(int bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Match).ThenInclude(m => m.HomeTeam)
            .Include(b => b.Match).ThenInclude(m => m.AwayTeam)
            .Include(b => b.Match).ThenInclude(m => m.Stadium)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Ticket)
            .Include(b => b.FoodItems).ThenInclude(bf => bf.FoodItem)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null) return;

        var section = _config.GetSection("Email");
        var host = section["SmtpHost"] ?? "smtp.gmail.com";
        var port = int.TryParse(section["SmtpPort"], out var p) ? p : 587;
        var username = section["Username"];
        var password = section["Password"];
        var fromEmail = string.IsNullOrEmpty(section["FromEmail"]) ? username : section["FromEmail"];
        var fromName = section["FromName"] ?? "MatchdayApi";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Thiếu cấu hình Email:Username / Email:Password. Đặt qua User Secrets, không hardcode vào appsettings.");
        }

        using var message = new MailMessage();
        message.From = new MailAddress(fromEmail!, fromName);
        message.To.Add(booking.User.Email);
        message.Subject = $"[MatchdayApi] Xác nhận đặt vé thành công — {booking.BookingCode}";
        message.IsBodyHtml = true;

        var view = AlternateView.CreateAlternateViewFromString(BuildHtmlBody(booking, out var qrImages), null, "text/html");
        foreach (var (cid, bytes) in qrImages)
        {
            var resource = new LinkedResource(new MemoryStream(bytes), "image/png") { ContentId = cid };
            view.LinkedResources.Add(resource);
        }
        message.AlternateViews.Add(view);

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };
        await client.SendMailAsync(message);
    }

    private string BuildHtmlBody(Models.Booking booking, out List<(string Cid, byte[] Bytes)> qrImages)
    {
        var seatsAmount = booking.BookingSeats.Sum(bs => bs.Price);
        var foodAmount = booking.FoodItems.Sum(bf => bf.UnitPrice * bf.Quantity);

        qrImages = new List<(string, byte[])>();
        var sb = new System.Text.StringBuilder();
        sb.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;background:#0A0C10;color:#F4F6FA;padding:24px;\">");
        sb.Append("<h2 style=\"color:#5B9CFF;\">Xác nhận đặt vé thành công!</h2>");
        sb.Append("<p>Xin chào ").Append(booking.User.FullName).Append(", cảm ơn bạn đã đặt vé tại MatchdayApi.</p>");
        sb.Append("<p><b>Mã đơn:</b> ").Append(booking.BookingCode).Append("<br/>");
        sb.Append("<b>Trận đấu:</b> ").Append(booking.Match.HomeTeam.Name).Append(" vs ").Append(booking.Match.AwayTeam.Name).Append("<br/>");
        sb.Append("<b>Thời gian:</b> ").Append(booking.Match.MatchDateTime.ToString("HH:mm dd/MM/yyyy")).Append("<br/>");
        sb.Append("<b>Sân:</b> ").Append(booking.Match.Stadium.Name).Append("<br/>");
        sb.Append("<b>Tiền vé:</b> ").Append(seatsAmount.ToString("N0")).Append("đ<br/>");
        if (booking.FoodItems.Count > 0)
        {
            sb.Append("<b>Tiền đồ ăn/thức uống:</b> ").Append(foodAmount.ToString("N0")).Append("đ<br/>");
        }
        sb.Append("<b>Tổng tiền:</b> ").Append(booking.TotalAmount.ToString("N0")).Append("đ</p>");

        var i = 0;
        foreach (var bs in booking.BookingSeats)
        {
            if (bs.Ticket is null) continue;
            i++;
            var cid = $"qr{i}";
            var bytes = _qr.GeneratePngBytes(bs.Ticket.QrCodeData ?? bs.Ticket.TicketCode);
            qrImages.Add((cid, bytes));

            sb.Append("<div style=\"margin-top:12px;padding:12px;border:1px solid #23272f;border-radius:8px;\">");
            sb.Append("<img src=\"cid:").Append(cid).Append("\" width=\"100\" height=\"100\" style=\"background:#fff;padding:4px;border-radius:6px;\" /><br/>");
            sb.Append("<b style=\"color:#2F6FED;\">").Append(bs.Ticket.TicketCode).Append("</b><br/>");
            sb.Append("Ghế ").Append(bs.Seat.RowLabel).Append(bs.Seat.SeatNumber);
            sb.Append("</div>");
        }

        if (booking.FoodItems.Count > 0)
        {
            sb.Append("<div style=\"margin-top:16px;\"><b>Đồ ăn/thức uống đã đặt:</b><ul>");
            foreach (var bf in booking.FoodItems)
            {
                sb.Append("<li>").Append(bf.FoodItem.Name).Append(" x").Append(bf.Quantity)
                  .Append(" — ").Append((bf.UnitPrice * bf.Quantity).ToString("N0")).Append("đ</li>");
            }
            sb.Append("</ul></div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }
}

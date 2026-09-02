using System.Text;
using MatchdayApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Services;

public class MockEmailService : IEmailService
{
    private readonly AppDbContext _db;
    private readonly IQrCodeService _qr;
    private readonly IWebHostEnvironment _env;

    public MockEmailService(AppDbContext db, IQrCodeService qr, IWebHostEnvironment env)
    {
        _db = db;
        _qr = qr;
        _env = env;
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

        var html = BuildEmailHtml(booking);

        var folder = Path.Combine(_env.WebRootPath, "sent-emails");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, $"{booking.BookingCode}.html");
        await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);

        Console.WriteLine($"[MockEmailService] Đã \"gửi\" (giả lập) email xác nhận cho {booking.User.Email} -> xem tại /sent-emails/{booking.BookingCode}.html");
    }

    private string BuildEmailHtml(Models.Booking booking)
    {
        var seatsAmount = booking.BookingSeats.Sum(bs => bs.Price);
        var foodAmount = booking.FoodItems.Sum(bf => bf.UnitPrice * bf.Quantity);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"UTF-8\" />");
        sb.Append("<title>Vé điện tử — ").Append(booking.BookingCode).Append("</title>");
        sb.Append("<style>");
        sb.Append("body{font-family:Segoe UI,Arial,sans-serif;background:#0A0C10;color:#F4F6FA;margin:0;padding:24px;}");
        sb.Append(".badge{display:inline-block;background:#c98a2b;color:#0A0C10;font-weight:600;font-size:11px;letter-spacing:.04em;padding:3px 10px;border-radius:999px;margin-bottom:14px;}");
        sb.Append(".card{background:#12151b;border:1px solid #23272f;border-radius:12px;padding:28px 32px;max-width:560px;margin:0 auto 16px;}");
        sb.Append("h1{font-size:20px;margin:0 0 4px;color:#5B9CFF;}");
        sb.Append("h2{font-size:16px;margin:0 0 12px;}");
        sb.Append(".hint{color:#9aa4b2;font-size:13px;margin-bottom:18px;}");
        sb.Append(".row{display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid #23272f;font-size:14px;}");
        sb.Append(".row span:first-child{color:#9aa4b2;}");
        sb.Append(".row.total{border-bottom:none;font-weight:600;color:#5B9CFF;padding-top:12px;}");
        sb.Append(".ticket{display:flex;align-items:center;gap:16px;background:#0A0C10;border:1px solid #23272f;border-radius:8px;padding:14px;margin-top:12px;}");
        sb.Append(".ticket img{width:100px;height:100px;background:#fff;border-radius:6px;padding:4px;}");
        sb.Append(".ticket .code{color:#2F6FED;font-weight:600;font-size:14px;}");
        sb.Append(".ticket .seat{color:#9aa4b2;font-size:13px;}");
        sb.Append(".food-row{display:flex;justify-content:space-between;padding:6px 0;font-size:14px;}");
        sb.Append(".food-row span:first-child{color:#F4F6FA;}");
        sb.Append(".food-row span:last-child{color:#9aa4b2;}");
        sb.Append("</style></head><body>");

        sb.Append("<div class=\"card\">");
        sb.Append("<span class=\"badge\">EMAIL GIẢ LẬP — MOCK MODE</span>");
        sb.Append("<h1>Xác nhận đặt vé thành công!</h1>");
        sb.Append("<p class=\"hint\">Xin chào ").Append(booking.User.FullName).Append(", cảm ơn bạn đã đặt vé tại MatchdayApi. Dưới đây là thông tin đơn hàng và vé điện tử của bạn.</p>");

        sb.Append("<div class=\"row\"><span>Mã đơn</span><span>").Append(booking.BookingCode).Append("</span></div>");
        sb.Append("<div class=\"row\"><span>Trận đấu</span><span>").Append(booking.Match.HomeTeam.Name).Append(" vs ").Append(booking.Match.AwayTeam.Name).Append("</span></div>");
        sb.Append("<div class=\"row\"><span>Thời gian</span><span>").Append(booking.Match.MatchDateTime.ToString("HH:mm dd/MM/yyyy")).Append("</span></div>");
        sb.Append("<div class=\"row\"><span>Sân vận động</span><span>").Append(booking.Match.Stadium.Name).Append("</span></div>");
        sb.Append("<div class=\"row\"><span>Tiền vé</span><span>").Append(seatsAmount.ToString("N0")).Append("đ</span></div>");
        if (booking.FoodItems.Count > 0)
        {
            sb.Append("<div class=\"row\"><span>Tiền đồ ăn/thức uống</span><span>").Append(foodAmount.ToString("N0")).Append("đ</span></div>");
        }
        sb.Append("<div class=\"row total\"><span>Tổng tiền</span><span>").Append(booking.TotalAmount.ToString("N0")).Append("đ</span></div>");
        sb.Append("</div>");

        sb.Append("<div class=\"card\"><h2>Vé điện tử (").Append(booking.BookingSeats.Count).Append(" vé)</h2>");
        foreach (var bs in booking.BookingSeats)
        {
            if (bs.Ticket is null) continue;
            var qrBytes = _qr.GeneratePngBytes(bs.Ticket.QrCodeData ?? bs.Ticket.TicketCode);
            var qrBase64 = Convert.ToBase64String(qrBytes);
            sb.Append("<div class=\"ticket\">");
            sb.Append("<img src=\"data:image/png;base64,").Append(qrBase64).Append("\" alt=\"QR\" />");
            sb.Append("<div><div class=\"code\">").Append(bs.Ticket.TicketCode).Append("</div>");
            sb.Append("<div class=\"seat\">Ghế ").Append(bs.Seat.RowLabel).Append(bs.Seat.SeatNumber)
              .Append(" — ").Append(bs.Price.ToString("N0")).Append("đ</div></div>");
            sb.Append("</div>");
        }
        sb.Append("</div>");

        if (booking.FoodItems.Count > 0)
        {
            sb.Append("<div class=\"card\"><h2>Đồ ăn/thức uống đã đặt</h2>");
            foreach (var bf in booking.FoodItems)
            {
                sb.Append("<div class=\"food-row\"><span>").Append(bf.FoodItem.Name).Append(" x").Append(bf.Quantity).Append("</span>");
                sb.Append("<span>").Append((bf.UnitPrice * bf.Quantity).ToString("N0")).Append("đ</span></div>");
            }
            sb.Append("</div>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }
}

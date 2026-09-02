using MatchdayApi.Data;
using MatchdayApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Ticket>> GenerateTicketsForBookingAsync(int bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Ticket)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null) return new List<Ticket>();

        foreach (var bookingSeat in booking.BookingSeats)
        {
            if (bookingSeat.Ticket is not null) continue; // đã có vé rồi — bỏ qua, tránh tạo trùng

            var ticketCode = $"{booking.BookingCode}-{bookingSeat.Seat.RowLabel}{bookingSeat.Seat.SeatNumber}";
            var ticket = new Ticket
            {
                BookingSeatId = bookingSeat.Id,
                TicketCode = ticketCode,
                QrCodeData = ticketCode // nội dung mã hoá vào QR chính là mã vé — đơn giản, đủ để tra cứu/check-in sau này
            };
            _db.Tickets.Add(ticket);
            bookingSeat.Ticket = ticket;
        }

        await _db.SaveChangesAsync();

        return booking.BookingSeats.Select(bs => bs.Ticket!).ToList();
    }
}

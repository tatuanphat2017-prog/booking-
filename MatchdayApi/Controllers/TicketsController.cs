using System.Security.Claims;
using MatchdayApi.Data;
using MatchdayApi.DTOs.Tickets;
using MatchdayApi.Enums;
using MatchdayApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

/// <summary>
/// Xem vé điện tử + tải ảnh QR (task #14). Vé chỉ tồn tại sau khi Booking đã thanh toán thành công
/// (do PaymentsController tự gọi ITicketService sinh vé lúc đó) — Booking còn Pending thì chưa có vé.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IQrCodeService _qr;

    public TicketsController(AppDbContext db, IQrCodeService qr)
    {
        _db = db;
        _qr = qr;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Danh sách vé của 1 Booking — chỉ chủ đơn hoặc Admin xem được.</summary>
    [HttpGet("booking/{bookingId:int}")]
    public async Task<ActionResult<List<TicketDto>>> GetByBooking(int bookingId)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đặt vé." });

        if (!User.IsInRole("Admin") && booking.UserId != CurrentUserId)
        {
            return Forbid();
        }

        if (booking.Status != BookingStatus.Paid)
        {
            return BadRequest(new { message = "Đơn chưa thanh toán xong nên chưa có vé điện tử." });
        }

        var tickets = await _db.Tickets
            .Include(t => t.BookingSeat).ThenInclude(bs => bs.Seat)
            .Where(t => t.BookingSeat.BookingId == bookingId)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                TicketCode = t.TicketCode,
                IsCheckedIn = t.IsCheckedIn,
                CheckedInAt = t.CheckedInAt,
                SeatId = t.BookingSeat.SeatId,
                RowLabel = t.BookingSeat.Seat.RowLabel,
                SeatNumber = t.BookingSeat.Seat.SeatNumber
            })
            .ToListAsync();

        return Ok(tickets);
    }

    /// <summary>Tải ảnh QR (PNG) của 1 vé cụ thể — chỉ chủ đơn hoặc Admin xem được.</summary>
    [HttpGet("{ticketId:int}/qr")]
    public async Task<IActionResult> GetQr(int ticketId)
    {
        var ticket = await _db.Tickets
            .Include(t => t.BookingSeat).ThenInclude(bs => bs.Booking)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket is null) return NotFound(new { message = "Không tìm thấy vé." });

        var booking = ticket.BookingSeat.Booking;
        if (!User.IsInRole("Admin") && booking.UserId != CurrentUserId)
        {
            return Forbid();
        }

        var png = _qr.GeneratePngBytes(ticket.QrCodeData ?? ticket.TicketCode);
        return File(png, "image/png");
    }
}

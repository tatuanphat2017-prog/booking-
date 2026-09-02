using System.Security.Claims;
using MatchdayApi.Data;
using MatchdayApi.DTOs.Refunds;
using MatchdayApi.Enums;
using MatchdayApi.Hubs;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

/// <summary>
/// Xem + duyệt/từ chối yêu cầu hủy vé/hoàn tiền (task #19). User tạo yêu cầu qua
/// <c>POST /api/Bookings/{id}/refund-request</c> (BookingsController) — controller này chỉ để XEM
/// (cả user lẫn Admin) và ADMIN DUYỆT/TỪ CHỐI.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RefundsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<SeatSelectionHub> _hub;

    public RefundsController(AppDbContext db, IHubContext<SeatSelectionHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Danh sách yêu cầu hoàn tiền của chính user đang đăng nhập.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<RefundDto>>> GetMine()
    {
        var userId = CurrentUserId;
        var refunds = await _db.Refunds
            .Include(r => r.Booking)
            .Where(r => r.Booking.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        return Ok(refunds.Select(ToDto).ToList());
    }

    /// <summary>Danh sách TẤT CẢ yêu cầu hoàn tiền — chỉ Admin. Lọc theo trạng thái: ?status=Pending|Approved|Rejected.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<RefundDto>>> GetAll([FromQuery] RefundStatus? status)
    {
        var query = _db.Refunds.Include(r => r.Booking).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var refunds = await query.OrderByDescending(r => r.RequestedAt).ToListAsync();
        return Ok(refunds.Select(ToDto).ToList());
    }

    /// <summary>
    /// Duyệt yêu cầu hoàn tiền — chỉ Admin. Chuyển Booking sang "Refunded", GIẢI PHÓNG các ghế đã đặt
    /// (xóa BookingSeat — cascade xóa luôn Ticket liên quan vì Ticket phụ thuộc 1-1 vào BookingSeat) để
    /// người khác đặt lại được, và báo real-time qua SignalR (SeatReleased) cho ai đang xem sơ đồ ghế.
    /// Việc chuyển khoản hoàn tiền thật cho khách làm THỦ CÔNG ngoài hệ thống (vd chuyển khoản ngân hàng) —
    /// hệ thống chỉ ghi nhận đã duyệt, không tự động hoàn tiền qua VNPay (VNPay sandbox không hỗ trợ
    /// hoàn tiền tự động cho tài khoản demo).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var refund = await _db.Refunds
            .Include(r => r.Booking).ThenInclude(b => b.BookingSeats)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (refund is null) return NotFound(new { message = "Không tìm thấy yêu cầu hoàn tiền." });
        if (refund.Status != RefundStatus.Pending)
        {
            return BadRequest(new { message = "Yêu cầu này đã được xử lý rồi." });
        }

        refund.Status = RefundStatus.Approved;
        refund.ProcessedAt = DateTime.UtcNow;
        refund.Booking.Status = BookingStatus.Refunded;

        var seatIds = refund.Booking.BookingSeats.Select(bs => bs.SeatId).ToList();
        var matchId = refund.Booking.MatchId;
        _db.BookingSeats.RemoveRange(refund.Booking.BookingSeats);

        await _db.SaveChangesAsync();

        foreach (var seatId in seatIds)
        {
            await _hub.Clients
                .Group(SeatSelectionHub.GroupName(matchId))
                .SendAsync("SeatReleased", seatId);
        }

        return NoContent();
    }

    /// <summary>Từ chối yêu cầu hoàn tiền — chỉ Admin. Booking vẫn giữ nguyên "Paid", ghế không bị giải phóng.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var refund = await _db.Refunds.FindAsync(id);
        if (refund is null) return NotFound(new { message = "Không tìm thấy yêu cầu hoàn tiền." });
        if (refund.Status != RefundStatus.Pending)
        {
            return BadRequest(new { message = "Yêu cầu này đã được xử lý rồi." });
        }

        refund.Status = RefundStatus.Rejected;
        refund.ProcessedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static RefundDto ToDto(Refund r) => new()
    {
        Id = r.Id,
        BookingId = r.BookingId,
        BookingCode = r.Booking.BookingCode,
        Reason = r.Reason,
        Status = r.Status,
        Amount = r.Amount,
        RequestedAt = r.RequestedAt,
        ProcessedAt = r.ProcessedAt
    };
}

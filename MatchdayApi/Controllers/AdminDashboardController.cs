using MatchdayApi.Data;
using MatchdayApi.DTOs.AdminDashboard;
using MatchdayApi.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

/// <summary>
/// Số liệu thống kê cho Admin — task #22. Toàn bộ endpoint chỉ Admin mới xem được.
/// "Doanh thu" trong toàn bộ controller này chỉ tính đơn đang ở trạng thái Paid — đơn Cancelled
/// hoặc Refunded không được tính vào doanh thu (tiền đã/sẽ được hoàn lại).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminDashboardController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Số liệu tổng quan: doanh thu, số đơn theo trạng thái, số vé, số yêu cầu hoàn tiền.</summary>
    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview()
    {
        var totalRevenue = await _db.Bookings
            .Where(b => b.Status == BookingStatus.Paid)
            .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

        var seatRevenue = await _db.BookingSeats
            .Where(bs => bs.Booking.Status == BookingStatus.Paid)
            .SumAsync(bs => (decimal?)bs.Price) ?? 0;

        var foodRevenue = await _db.BookingFoodItems
            .Where(bf => bf.Booking.Status == BookingStatus.Paid)
            .SumAsync(bf => (decimal?)(bf.Quantity * bf.UnitPrice)) ?? 0;

        var bookingCounts = await _db.Bookings
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int CountOf(BookingStatus s) => bookingCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        var refundCounts = await _db.Refunds
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int RefundCountOf(RefundStatus s) => refundCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        var totalRefundedAmount = await _db.Refunds
            .Where(r => r.Status == RefundStatus.Approved)
            .SumAsync(r => (decimal?)r.Amount) ?? 0;

        var totalTicketsSold = await _db.Tickets.CountAsync();

        return Ok(new AdminOverviewDto
        {
            TotalRevenue = totalRevenue,
            SeatRevenue = seatRevenue,
            FoodRevenue = foodRevenue,
            TotalBookings = bookingCounts.Sum(x => x.Count),
            PendingBookings = CountOf(BookingStatus.Pending),
            PaidBookings = CountOf(BookingStatus.Paid),
            CancelledBookings = CountOf(BookingStatus.Cancelled),
            RefundedBookings = CountOf(BookingStatus.Refunded),
            TotalTicketsSold = totalTicketsSold,
            PendingRefundRequests = RefundCountOf(RefundStatus.Pending),
            ApprovedRefunds = RefundCountOf(RefundStatus.Approved),
            RejectedRefunds = RefundCountOf(RefundStatus.Rejected),
            TotalRefundedAmount = totalRefundedAmount
        });
    }

    /// <summary>Doanh thu + số vé bán theo từng trận, sắp theo doanh thu giảm dần.</summary>
    [HttpGet("revenue-by-match")]
    public async Task<ActionResult<List<MatchRevenueDto>>> GetRevenueByMatch([FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 200);

        var bookings = await _db.Bookings
            .Where(b => b.Status == BookingStatus.Paid)
            .Include(b => b.Match).ThenInclude(m => m.HomeTeam)
            .Include(b => b.Match).ThenInclude(m => m.AwayTeam)
            .Include(b => b.BookingSeats)
            .Include(b => b.FoodItems)
            .ToListAsync();

        var result = bookings
            .GroupBy(b => b.MatchId)
            .Select(g =>
            {
                var match = g.First().Match;
                var seatRevenue = g.Sum(b => b.BookingSeats.Sum(bs => bs.Price));
                var foodRevenue = g.Sum(b => b.FoodItems.Sum(f => f.Quantity * f.UnitPrice));
                return new MatchRevenueDto
                {
                    MatchId = g.Key,
                    MatchDateTime = match.MatchDateTime,
                    HomeTeamName = match.HomeTeam.Name,
                    AwayTeamName = match.AwayTeam.Name,
                    TicketsSold = g.Sum(b => b.BookingSeats.Count),
                    SeatRevenue = seatRevenue,
                    FoodRevenue = foodRevenue,
                    TotalRevenue = seatRevenue + foodRevenue
                };
            })
            .OrderByDescending(m => m.TotalRevenue)
            .Take(take)
            .ToList();

        return Ok(result);
    }

    /// <summary>Doanh thu theo từng ngày trong N ngày gần nhất (mặc định 30 ngày) — dùng vẽ biểu đồ.</summary>
    [HttpGet("revenue-by-day")]
    public async Task<ActionResult<List<DailyRevenueDto>>> GetRevenueByDay([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 365);
        var since = DateTime.UtcNow.AddDays(-days);

        var payments = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Success && p.PaidAt != null && p.PaidAt >= since)
            .ToListAsync();

        var result = payments
            .GroupBy(p => p.PaidAt!.Value.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(p => p.Amount),
                PaymentCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Ok(result);
    }

    /// <summary>Món đồ ăn/thức uống bán chạy nhất (theo doanh thu), chỉ tính đơn đang Paid.</summary>
    [HttpGet("top-food-items")]
    public async Task<ActionResult<List<TopFoodItemDto>>> GetTopFoodItems([FromQuery] int take = 10)
    {
        take = Math.Clamp(take, 1, 100);

        var items = await _db.BookingFoodItems
            .Where(bf => bf.Booking.Status == BookingStatus.Paid)
            .Include(bf => bf.FoodItem)
            .ToListAsync();

        var result = items
            .GroupBy(bf => bf.FoodItemId)
            .Select(g => new TopFoodItemDto
            {
                FoodItemId = g.Key,
                Name = g.First().FoodItem.Name,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(take)
            .ToList();

        return Ok(result);
    }
}

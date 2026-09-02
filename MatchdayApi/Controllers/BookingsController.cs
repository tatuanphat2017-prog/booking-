using System.Security.Claims;
using MatchdayApi.Data;
using MatchdayApi.DTOs.Bookings;
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
/// Tạo và xem đơn đặt vé (task #12 — phần chốt Booking sau khi chọn ghế qua SignalR).
/// Toàn bộ endpoint đều yêu cầu đăng nhập (User hoặc Admin đều đặt vé được).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<SeatSelectionHub> _hub;

    public BookingsController(AppDbContext db, IHubContext<SeatSelectionHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Chốt đặt vé cho các ghế mà chính user này đang GIỮ qua SignalR (SeatSelectionHub.HoldSeat).
    /// Nếu ghế không còn được user này giữ (hết hạn/chưa từng giữ) hoặc đã bị người khác đặt mất
    /// trong lúc thao tác, trả về lỗi rõ ràng để frontend yêu cầu người dùng chọn lại.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingDto dto)
    {
        var seatIds = (dto.SeatIds ?? new List<int>()).Distinct().ToList();
        if (seatIds.Count == 0)
        {
            return BadRequest(new { message = "Phải chọn ít nhất 1 ghế." });
        }

        var match = await _db.Matches.FindAsync(dto.MatchId);
        if (match is null) return NotFound(new { message = "Không tìm thấy trận đấu." });

        if (match.Status == MatchStatus.Finished)
        {
            return BadRequest(new { message = "Trận đấu đã kết thúc, không thể đặt vé." });
        }

        var userId = CurrentUserId;
        var now = DateTime.UtcNow;

        var seats = await _db.Seats
            .Include(s => s.SeatBlock)
            .Where(s => seatIds.Contains(s.Id))
            .ToListAsync();

        if (seats.Count != seatIds.Count)
        {
            return BadRequest(new { message = "Có ghế không tồn tại." });
        }

        if (seats.Any(s => s.SeatBlock.StadiumId != match.StadiumId))
        {
            return BadRequest(new { message = "Có ghế không thuộc sân thi đấu của trận này." });
        }

        // Ghế phải đang được CHÍNH user này giữ (qua Hub) và còn hạn thì mới cho chốt đặt vé —
        // đảm bảo tính nhất quán giữa bước "chọn ghế real-time" và bước "đặt vé".
        var holds = await _db.SeatHolds
            .Where(h => h.MatchId == dto.MatchId && seatIds.Contains(h.SeatId))
            .ToListAsync();

        foreach (var seatId in seatIds)
        {
            var hold = holds.FirstOrDefault(h => h.SeatId == seatId);
            if (hold is null || hold.UserId != userId || hold.ExpireAt <= now)
            {
                return Conflict(new
                {
                    message = $"Ghế (Id {seatId}) không còn được bạn giữ chỗ (có thể đã hết hạn 5 phút), vui lòng chọn lại ghế."
                });
            }
        }

        var alreadyBooked = await _db.BookingSeats
            .AnyAsync(bs => bs.MatchId == dto.MatchId && seatIds.Contains(bs.SeatId));
        if (alreadyBooked)
        {
            return Conflict(new { message = "Rất tiếc, có ghế vừa bị người khác đặt mất, vui lòng chọn lại." });
        }

        var prices = await _db.MatchTicketPrices
            .Where(p => p.MatchId == dto.MatchId)
            .ToDictionaryAsync(p => p.SeatBlockId, p => p.Price);

        var missingPriceBlock = seats.FirstOrDefault(s => !prices.ContainsKey(s.SeatBlockId));
        if (missingPriceBlock is not null)
        {
            return BadRequest(new
            {
                message = $"Khối ghế '{missingPriceBlock.SeatBlock.Name}' chưa được Admin thiết lập giá vé cho trận này."
            });
        }

        var booking = new Booking
        {
            BookingCode = GenerateBookingCode(),
            Status = BookingStatus.Pending,
            UserId = userId,
            MatchId = dto.MatchId,
            CreatedAt = now,
            TotalAmount = seats.Sum(s => prices[s.SeatBlockId])
        };

        foreach (var seat in seats)
        {
            booking.BookingSeats.Add(new BookingSeat
            {
                SeatId = seat.Id,
                MatchId = dto.MatchId,
                Price = prices[seat.SeatBlockId]
            });
        }

        _db.Bookings.Add(booking);
        _db.SeatHolds.RemoveRange(holds);

        await _db.SaveChangesAsync();

        // Báo real-time cho mọi người đang xem sơ đồ ghế trận này: các ghế vừa chốt đã "Booked".
        foreach (var seatId in seatIds)
        {
            await _hub.Clients
                .Group(SeatSelectionHub.GroupName(dto.MatchId))
                .SendAsync("SeatBooked", seatId);
        }

        var resultDto = await BuildBookingDto(booking.Id);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, resultDto);
    }

    /// <summary>Danh sách đơn đặt vé của chính user đang đăng nhập.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<BookingDto>>> GetMine()
    {
        var userId = CurrentUserId;
        var bookingIds = await _db.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => b.Id)
            .ToListAsync();

        var result = new List<BookingDto>();
        foreach (var id in bookingIds)
        {
            var dto = await BuildBookingDto(id);
            if (dto is not null) result.Add(dto);
        }

        return Ok(result);
    }

    /// <summary>
    /// Đặt/cập nhật giỏ đồ ăn-thức uống cho 1 đơn đang Pending (task #17) — gọi lại nhiều lần sẽ THAY THẾ
    /// toàn bộ giỏ đồ ăn cũ bằng danh sách mới (không cộng dồn), đơn giản hoá việc sửa giỏ hàng.
    /// Chỉ áp dụng khi đơn còn Pending — đã thanh toán/hủy thì không sửa được nữa.
    /// Tự cập nhật lại Booking.TotalAmount = tiền ghế + tiền đồ ăn — PaymentsController dùng thẳng giá trị
    /// này khi tạo link thanh toán VNPay nên không cần sửa gì thêm ở bước thanh toán.
    /// </summary>
    [HttpPut("{id:int}/food-items")]
    public async Task<ActionResult<BookingDto>> SetFoodItems(int id, SetFoodItemsDto dto)
    {
        var booking = await _db.Bookings
            .Include(b => b.BookingSeats)
            .Include(b => b.FoodItems)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đặt vé." });
        if (booking.UserId != CurrentUserId) return Forbid();

        if (booking.Status != BookingStatus.Pending)
        {
            return BadRequest(new { message = "Đơn đã thanh toán hoặc đã hủy, không thể sửa đồ ăn/thức uống." });
        }

        var items = (dto.Items ?? new()).Where(i => i.Quantity > 0).ToList();
        var foodItemIds = items.Select(i => i.FoodItemId).Distinct().ToList();

        var foodItems = await _db.FoodItems
            .Where(f => foodItemIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id);

        var missingIds = foodItemIds.Where(fid => !foodItems.ContainsKey(fid)).ToList();
        if (missingIds.Count > 0)
        {
            return BadRequest(new { message = $"Có món ăn/thức uống không tồn tại (Id: {string.Join(", ", missingIds)})." });
        }

        var unavailable = items.FirstOrDefault(i => !foodItems[i.FoodItemId].IsAvailable);
        if (unavailable is not null)
        {
            return BadRequest(new { message = $"Món '{foodItems[unavailable.FoodItemId].Name}' hiện đã ngừng bán." });
        }

        // Thay thế toàn bộ giỏ đồ ăn cũ bằng danh sách mới.
        _db.BookingFoodItems.RemoveRange(booking.FoodItems);
        booking.FoodItems.Clear();

        var seatsAmount = booking.BookingSeats.Sum(bs => bs.Price);
        decimal foodAmount = 0;
        foreach (var item in items)
        {
            var food = foodItems[item.FoodItemId];
            foodAmount += food.Price * item.Quantity;
            booking.FoodItems.Add(new BookingFoodItem
            {
                FoodItemId = food.Id,
                Quantity = item.Quantity,
                UnitPrice = food.Price // snapshot giá tại thời điểm đặt, tránh bị ảnh hưởng nếu Admin đổi giá sau này
            });
        }

        booking.TotalAmount = seatsAmount + foodAmount;

        await _db.SaveChangesAsync();

        var resultDto = await BuildBookingDto(booking.Id);
        return Ok(resultDto);
    }

    /// <summary>
    /// Hủy 1 đơn CHƯA thanh toán (task #19) — chỉ áp dụng khi Status = Pending. Giải phóng NGAY các ghế
    /// đã chốt (xóa BookingSeat) để người khác đặt lại được, và báo real-time qua SignalR (SeatReleased)
    /// cho ai đang xem sơ đồ ghế trận đó. Đơn ĐÃ thanh toán phải dùng {id}/refund-request — cần Admin duyệt.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.BookingSeats)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đặt vé." });
        if (booking.UserId != CurrentUserId) return Forbid();

        if (booking.Status != BookingStatus.Pending)
        {
            return BadRequest(new
            {
                message = "Chỉ có thể hủy đơn đang chờ thanh toán. Đơn đã thanh toán phải gửi yêu cầu hoàn tiền (POST .../refund-request)."
            });
        }

        var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
        var matchId = booking.MatchId;
        _db.BookingSeats.RemoveRange(booking.BookingSeats);
        booking.Status = BookingStatus.Cancelled;

        await _db.SaveChangesAsync();

        foreach (var seatId in seatIds)
        {
            await _hub.Clients
                .Group(SeatSelectionHub.GroupName(matchId))
                .SendAsync("SeatReleased", seatId);
        }

        return NoContent();
    }

    /// <summary>
    /// Gửi yêu cầu hủy vé/hoàn tiền cho 1 đơn ĐÃ thanh toán (task #19) — KHÔNG hoàn tiền/giải phóng ghế
    /// ngay, chỉ tạo 1 bản ghi Refund ở trạng thái Pending, chờ Admin duyệt qua RefundsController.
    /// </summary>
    [HttpPost("{id:int}/refund-request")]
    public async Task<ActionResult<RefundDto>> RequestRefund(int id, RefundRequestDto dto)
    {
        var booking = await _db.Bookings
            .Include(b => b.Match)
            .Include(b => b.Refunds)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đặt vé." });
        if (booking.UserId != CurrentUserId) return Forbid();

        if (booking.Status != BookingStatus.Paid)
        {
            return BadRequest(new { message = "Chỉ có thể yêu cầu hoàn tiền cho đơn đã thanh toán." });
        }

        if (booking.Match.Status == MatchStatus.Finished)
        {
            return BadRequest(new { message = "Trận đấu đã kết thúc, không thể yêu cầu hoàn tiền." });
        }

        if (booking.Refunds.Any(r => r.Status == RefundStatus.Pending))
        {
            return BadRequest(new { message = "Đơn này đã có 1 yêu cầu hoàn tiền đang chờ xử lý." });
        }

        var refund = new Refund
        {
            BookingId = booking.Id,
            Reason = dto.Reason,
            Status = RefundStatus.Pending,
            Amount = booking.TotalAmount,
            RequestedAt = DateTime.UtcNow
        };

        _db.Refunds.Add(refund);
        await _db.SaveChangesAsync();

        return Ok(new RefundDto
        {
            Id = refund.Id,
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            Reason = refund.Reason,
            Status = refund.Status,
            Amount = refund.Amount,
            RequestedAt = refund.RequestedAt,
            ProcessedAt = refund.ProcessedAt
        });
    }

    /// <summary>Chi tiết 1 đơn đặt vé — chỉ chủ đơn hoặc Admin mới xem được.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDto>> GetById(int id)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đặt vé." });

        if (!User.IsInRole("Admin") && booking.UserId != CurrentUserId)
        {
            return Forbid();
        }

        var dto = await BuildBookingDto(id);
        return Ok(dto);
    }

    private async Task<BookingDto?> BuildBookingDto(int bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Match).ThenInclude(m => m.HomeTeam)
            .Include(b => b.Match).ThenInclude(m => m.AwayTeam)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat).ThenInclude(s => s.SeatBlock)
            .Include(b => b.FoodItems).ThenInclude(bf => bf.FoodItem)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null) return null;

        var seatsAmount = booking.BookingSeats.Sum(bs => bs.Price);
        var foodAmount = booking.FoodItems.Sum(bf => bf.UnitPrice * bf.Quantity);

        return new BookingDto
        {
            Id = booking.Id,
            BookingCode = booking.BookingCode,
            Status = booking.Status,
            TotalAmount = booking.TotalAmount,
            SeatsAmount = seatsAmount,
            FoodAmount = foodAmount,
            CreatedAt = booking.CreatedAt,
            MatchId = booking.MatchId,
            HomeTeamName = booking.Match.HomeTeam.Name,
            AwayTeamName = booking.Match.AwayTeam.Name,
            MatchDateTime = booking.Match.MatchDateTime,
            Seats = booking.BookingSeats.Select(bs => new BookingSeatItemDto
            {
                SeatId = bs.SeatId,
                RowLabel = bs.Seat.RowLabel,
                SeatNumber = bs.Seat.SeatNumber,
                SeatBlockName = bs.Seat.SeatBlock.Name,
                Price = bs.Price
            }).ToList(),
            FoodItems = booking.FoodItems.Select(bf => new BookingFoodItemDto
            {
                FoodItemId = bf.FoodItemId,
                Name = bf.FoodItem.Name,
                Quantity = bf.Quantity,
                UnitPrice = bf.UnitPrice
            }).ToList()
        };
    }

    private static string GenerateBookingCode()
    {
        return "BK" + DateTime.UtcNow.ToString("yyMMddHHmmss") + Random.Shared.Next(100, 999);
    }
}

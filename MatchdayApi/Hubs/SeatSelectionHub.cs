using System.Security.Claims;
using MatchdayApi.Data;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Hubs;

/// <summary>
/// Hub SignalR cho việc chọn ghế real-time (task #12). Luồng hoạt động:
/// 1. Client kết nối tới hub kèm JWT token, gọi JoinMatch(matchId) để vào "phòng" của trận đó.
/// 2. Khi user bấm chọn 1 ghế, client gọi HoldSeat(matchId, seatId) — server kiểm tra ghế còn
///    trống không, nếu được thì tạo/gia hạn SeatHold rồi báo cho TẤT CẢ client trong phòng
///    biết ghế này vừa bị giữ (để họ tô màu ghế đó là "đang được người khác chọn").
/// 3. Khi user bỏ chọn, client gọi ReleaseSeat để nhả ghế ra.
/// 4. Ghế giữ quá 5 phút mà chưa đặt vé sẽ tự động được dọn bởi SeatHoldCleanupService
///    (chạy nền, xem Services/SeatHoldCleanupService.cs) và cũng báo real-time cho mọi người.
/// 5. Khi đặt vé thành công (BookingsController), ghế chuyển hẳn sang trạng thái "Booked"
///    và cũng được báo qua hub này (sự kiện SeatBooked).
/// </summary>
[Authorize]
public class SeatSelectionHub : Hub
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;

    public SeatSelectionHub(AppDbContext db)
    {
        _db = db;
    }

    public static string GroupName(int matchId) => $"match-{matchId}";

    private int CurrentUserId
    {
        get
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return claim is null ? 0 : int.Parse(claim);
        }
    }

    /// <summary>Client gọi khi mở màn hình chọn ghế của 1 trận, để nhận broadcast real-time của đúng trận đó.</summary>
    public async Task JoinMatch(int matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(matchId));
    }

    /// <summary>Client gọi khi rời màn hình chọn ghế (đóng tab, chuyển trang...).</summary>
    public async Task LeaveMatch(int matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(matchId));
    }

    /// <summary>Thử giữ tạm 1 ghế trong 5 phút. Gửi HoldConfirmed/HoldRejected về riêng người gọi,
    /// và SeatHeld cho cả phòng nếu giữ thành công.</summary>
    public async Task HoldSeat(int matchId, int seatId)
    {
        var userId = CurrentUserId;
        var now = DateTime.UtcNow;

        var alreadyBooked = await _db.BookingSeats.AnyAsync(bs => bs.SeatId == seatId && bs.MatchId == matchId);
        if (alreadyBooked)
        {
            await Clients.Caller.SendAsync("HoldRejected", seatId, "Ghế này đã có người đặt vé.");
            return;
        }

        var existingHold = await _db.SeatHolds
            .FirstOrDefaultAsync(h => h.SeatId == seatId && h.MatchId == matchId);

        if (existingHold is not null && existingHold.ExpireAt > now && existingHold.UserId != userId)
        {
            await Clients.Caller.SendAsync("HoldRejected", seatId, "Ghế đang được người khác chọn, vui lòng chọn ghế khác.");
            return;
        }

        if (existingHold is not null)
        {
            // Ghế do chính mình giữ trước đó (hoặc đã hết hạn) -> gia hạn thêm 5 phút.
            existingHold.UserId = userId;
            existingHold.ConnectionId = Context.ConnectionId;
            existingHold.ExpireAt = now.Add(HoldDuration);
        }
        else
        {
            _db.SeatHolds.Add(new SeatHold
            {
                MatchId = matchId,
                SeatId = seatId,
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                ExpireAt = now.Add(HoldDuration)
            });
        }

        await _db.SaveChangesAsync();

        await Clients.Caller.SendAsync("HoldConfirmed", seatId, now.Add(HoldDuration));
        await Clients.OthersInGroup(GroupName(matchId)).SendAsync("SeatHeld", seatId);
    }

    /// <summary>Nhả 1 ghế đang giữ (user đổi ý không chọn ghế đó nữa).</summary>
    public async Task ReleaseSeat(int matchId, int seatId)
    {
        var userId = CurrentUserId;
        var hold = await _db.SeatHolds
            .FirstOrDefaultAsync(h => h.SeatId == seatId && h.MatchId == matchId && h.UserId == userId);

        if (hold is not null)
        {
            _db.SeatHolds.Remove(hold);
            await _db.SaveChangesAsync();
        }

        await Clients.Group(GroupName(matchId)).SendAsync("SeatReleased", seatId);
    }

    /// <summary>Đóng tab / mất kết nối đột ngột -> tự nhả hết ghế đang giữ bởi connection này.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var holds = await _db.SeatHolds
            .Where(h => h.ConnectionId == Context.ConnectionId)
            .ToListAsync();

        if (holds.Count > 0)
        {
            _db.SeatHolds.RemoveRange(holds);
            await _db.SaveChangesAsync();

            foreach (var hold in holds)
            {
                await Clients.Group(GroupName(hold.MatchId)).SendAsync("SeatReleased", hold.SeatId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}

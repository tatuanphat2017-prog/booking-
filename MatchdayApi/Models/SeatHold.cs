namespace MatchdayApi.Models;

/// <summary>
/// Ghế đang được 1 user giữ tạm thời qua SignalR trong lúc chọn ghế (chưa thanh toán).
/// Record này sẽ bị xóa khi hết hạn (ExpireAt) hoặc khi Booking được tạo thành công.
/// </summary>
public class SeatHold
{
    public int Id { get; set; }
    public string? ConnectionId { get; set; } // SignalR connection id
    public DateTime ExpireAt { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

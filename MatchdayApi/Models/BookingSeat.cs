namespace MatchdayApi.Models;

/// <summary>1 ghế cụ thể trong 1 đơn đặt vé</summary>
public class BookingSeat
{
    public int Id { get; set; }
    public decimal Price { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    /// <summary>
    /// Lưu thêm MatchId (suy ra từ Booking.MatchId) để tạo unique index (SeatId, MatchId)
    /// ở tầng DB — chặn 1 ghế bị bán trùng trong cùng 1 trận. Set giá trị này = Booking.MatchId
    /// khi tạo BookingSeat trong service, đừng để rỗng.
    /// </summary>
    public int MatchId { get; set; }

    public Ticket? Ticket { get; set; }
}

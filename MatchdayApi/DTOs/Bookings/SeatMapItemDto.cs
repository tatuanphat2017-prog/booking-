namespace MatchdayApi.DTOs.Bookings;

/// <summary>1 ô ghế trong sơ đồ chọn ghế của 1 trận đấu.</summary>
public class SeatMapItemDto
{
    public int SeatId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public int SeatBlockId { get; set; }
    public string SeatBlockName { get; set; } = string.Empty;

    /// <summary>Null nếu Admin chưa đặt giá vé cho khối ghế này ở trận đấu này.</summary>
    public decimal? Price { get; set; }

    /// <summary>"Available" | "Held" | "Booked"</summary>
    public string Status { get; set; } = "Available";

    /// <summary>True nếu ghế đang được chính người gọi API này giữ (chỉ có ý nghĩa khi đã đăng nhập).</summary>
    public bool IsHeldByMe { get; set; }
}

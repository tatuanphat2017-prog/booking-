using MatchdayApi.Enums;

namespace MatchdayApi.Models;

/// <summary>Yêu cầu hủy vé / hoàn tiền</summary>
public class Refund
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public decimal Amount { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
}

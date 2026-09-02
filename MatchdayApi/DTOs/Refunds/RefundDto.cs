using MatchdayApi.Enums;

namespace MatchdayApi.DTOs.Refunds;

public class RefundDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

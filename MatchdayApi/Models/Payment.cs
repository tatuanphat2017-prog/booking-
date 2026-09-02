using MatchdayApi.Enums;

namespace MatchdayApi.Models;

/// <summary>Giao dịch thanh toán (VNPay)</summary>
public class Payment
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "VNPay";
    public string? VnpTransactionId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
}

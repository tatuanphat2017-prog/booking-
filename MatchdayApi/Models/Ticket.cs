namespace MatchdayApi.Models;

/// <summary>Vé điện tử ứng với 1 ghế đã thanh toán thành công</summary>
public class Ticket
{
    public int Id { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string? QrCodeData { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }

    public int BookingSeatId { get; set; }
    public BookingSeat BookingSeat { get; set; } = null!;
}

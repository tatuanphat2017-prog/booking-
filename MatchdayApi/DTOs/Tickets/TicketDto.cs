namespace MatchdayApi.DTOs.Tickets;

public class TicketDto
{
    public int Id { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public int SeatId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
}

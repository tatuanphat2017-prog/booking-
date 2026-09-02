namespace MatchdayApi.DTOs.Seats;

public class SeatDto
{
    public int Id { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public int SeatBlockId { get; set; }
    public string SeatBlockName { get; set; } = string.Empty;
}

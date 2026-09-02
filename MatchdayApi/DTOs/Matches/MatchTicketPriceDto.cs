namespace MatchdayApi.DTOs.Matches;

public class MatchTicketPriceDto
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int SeatBlockId { get; set; }
    public string SeatBlockName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

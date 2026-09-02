namespace MatchdayApi.Models;

/// <summary>Giá vé theo từng trận (mỗi trận có thể có giá khác nhau theo khối ghế)</summary>
public class MatchTicketPrice
{
    public int Id { get; set; }
    public decimal Price { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int SeatBlockId { get; set; }
    public SeatBlock SeatBlock { get; set; } = null!;
}

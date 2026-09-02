namespace MatchdayApi.Models;

/// <summary>Chỉ số của 1 cầu thủ trong 1 trận cụ thể</summary>
public class PlayerMatchStat
{
    public int Id { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int MinutesPlayed { get; set; }
    public decimal Rating { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
}

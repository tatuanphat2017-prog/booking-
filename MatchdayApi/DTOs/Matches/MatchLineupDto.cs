namespace MatchdayApi.DTOs.Matches;

/// <summary>Đội hình của cả 2 đội trong 1 trận — đội nào chưa được xếp thì null.</summary>
public class MatchLineupDto
{
    public int MatchId { get; set; }
    public TeamLineupDto? Home { get; set; }
    public TeamLineupDto? Away { get; set; }
}

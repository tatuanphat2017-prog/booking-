namespace MatchdayApi.Models;

/// <summary>Đội hình ra sân của 1 đội trong 1 trận (sơ đồ chiến thuật)</summary>
public class MatchTeamLineup
{
    public int Id { get; set; }
    public string Formation { get; set; } = string.Empty; // vd: 4-3-3

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public ICollection<LineupPlayer> Players { get; set; } = new List<LineupPlayer>();
}

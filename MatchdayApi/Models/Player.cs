namespace MatchdayApi.Models;

/// <summary>Cầu thủ</summary>
public class Player
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int JerseyNumber { get; set; }
    public string Position { get; set; } = string.Empty; // GK, CB, LB, RB, CM, LW, RW, ST...
    public DateTime? DateOfBirth { get; set; }
    public string? PhotoUrl { get; set; }

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public ICollection<LineupPlayer> LineupAppearances { get; set; } = new List<LineupPlayer>();
    public ICollection<PlayerMatchStat> MatchStats { get; set; } = new List<PlayerMatchStat>();
}

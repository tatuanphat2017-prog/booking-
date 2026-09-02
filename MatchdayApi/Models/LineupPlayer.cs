namespace MatchdayApi.Models;

/// <summary>1 cầu thủ trong đội hình ra sân của 1 trận, kèm tọa độ vẽ trên sơ đồ sân</summary>
public class LineupPlayer
{
    public int Id { get; set; }
    public double PositionX { get; set; } // % chiều ngang sân (0-100)
    public double PositionY { get; set; } // % chiều dọc sân (0-100)
    public bool IsStarting { get; set; } = true; // đá chính hay dự bị

    public int MatchTeamLineupId { get; set; }
    public MatchTeamLineup MatchTeamLineup { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
}

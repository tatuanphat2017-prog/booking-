namespace MatchdayApi.DTOs.Matches;

/// <summary>1 cầu thủ trong đội hình ra sân, kèm tọa độ vẽ trên sơ đồ sân (task #20)</summary>
public class LineupPlayerDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int JerseyNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public bool IsStarting { get; set; }
}

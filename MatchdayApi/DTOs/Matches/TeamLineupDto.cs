namespace MatchdayApi.DTOs.Matches;

public class TeamLineupDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Formation { get; set; } = string.Empty;
    public List<LineupPlayerDto> Players { get; set; } = new();
}

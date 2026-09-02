using MatchdayApi.Enums;

namespace MatchdayApi.DTOs.Matches;

public class MatchDto
{
    public int Id { get; set; }
    public DateTime MatchDateTime { get; set; }
    public MatchStatus Status { get; set; }

    public int HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;

    public int AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = string.Empty;

    public int StadiumId { get; set; }
    public string StadiumName { get; set; } = string.Empty;
}

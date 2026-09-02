using System.ComponentModel.DataAnnotations;
using MatchdayApi.Enums;

namespace MatchdayApi.DTOs.Matches;

public class MatchUpsertDto
{
    [Required]
    public DateTime MatchDateTime { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Upcoming;

    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int StadiumId { get; set; }
}

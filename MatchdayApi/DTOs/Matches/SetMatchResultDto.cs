using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Matches;

public class SetMatchResultDto
{
    [Range(0, 99)] public int HomeScore { get; set; }
    [Range(0, 99)] public int AwayScore { get; set; }
    [Range(0, 100)] public int PossessionHome { get; set; } = 50;
    [Range(0, 100)] public int PossessionAway { get; set; } = 50;
    [Range(0, 99)] public int ShotsHome { get; set; }
    [Range(0, 99)] public int ShotsAway { get; set; }
    [Range(0, 20)] public int YellowCardsHome { get; set; }
    [Range(0, 20)] public int YellowCardsAway { get; set; }
    [Range(0, 10)] public int RedCardsHome { get; set; }
    [Range(0, 10)] public int RedCardsAway { get; set; }
    [Range(0, 30)] public int CornersHome { get; set; }
    [Range(0, 30)] public int CornersAway { get; set; }
}

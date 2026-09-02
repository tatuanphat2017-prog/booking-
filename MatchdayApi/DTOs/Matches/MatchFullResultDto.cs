namespace MatchdayApi.DTOs.Matches;

public class MatchFullResultDto
{
    public int MatchId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int PossessionHome { get; set; }
    public int PossessionAway { get; set; }
    public int ShotsHome { get; set; }
    public int ShotsAway { get; set; }
    public int YellowCardsHome { get; set; }
    public int YellowCardsAway { get; set; }
    public int RedCardsHome { get; set; }
    public int RedCardsAway { get; set; }
    public int CornersHome { get; set; }
    public int CornersAway { get; set; }
}

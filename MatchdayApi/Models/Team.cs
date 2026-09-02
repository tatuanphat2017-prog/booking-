namespace MatchdayApi.Models;

/// <summary>Đội bóng</summary>
public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }

    public int? HomeStadiumId { get; set; }
    public Stadium? HomeStadium { get; set; }

    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
    public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
    public ICollection<MatchTeamLineup> Lineups { get; set; } = new List<MatchTeamLineup>();
}

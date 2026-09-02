using MatchdayApi.Enums;

namespace MatchdayApi.Models;

/// <summary>Trận đấu</summary>
public class Match
{
    public int Id { get; set; }
    public DateTime MatchDateTime { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Upcoming;

    public int HomeTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;

    public int AwayTeamId { get; set; }
    public Team AwayTeam { get; set; } = null!;

    public int StadiumId { get; set; }
    public Stadium Stadium { get; set; } = null!;

    public MatchResult? Result { get; set; }
    public ICollection<MatchTicketPrice> TicketPrices { get; set; } = new List<MatchTicketPrice>();
    public ICollection<MatchTeamLineup> Lineups { get; set; } = new List<MatchTeamLineup>();
    public ICollection<PlayerMatchStat> PlayerStats { get; set; } = new List<PlayerMatchStat>();
    public ICollection<SeatHold> SeatHolds { get; set; } = new List<SeatHold>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

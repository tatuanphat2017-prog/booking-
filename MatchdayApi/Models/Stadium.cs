namespace MatchdayApi.Models;

/// <summary>Sân vận động</summary>
public class Stadium
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Capacity { get; set; }

    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<SeatBlock> SeatBlocks { get; set; } = new List<SeatBlock>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}

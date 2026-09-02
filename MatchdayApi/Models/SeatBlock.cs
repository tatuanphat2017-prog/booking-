namespace MatchdayApi.Models;

/// <summary>Khối ghế trong sân (Khối A, Khối VIP...)</summary>
public class SeatBlock
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }

    public int StadiumId { get; set; }
    public Stadium Stadium { get; set; } = null!;

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<MatchTicketPrice> MatchTicketPrices { get; set; } = new List<MatchTicketPrice>();
}

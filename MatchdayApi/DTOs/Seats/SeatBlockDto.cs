namespace MatchdayApi.DTOs.Seats;

public class SeatBlockDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int StadiumId { get; set; }
    public string StadiumName { get; set; } = string.Empty;
    public int SeatCount { get; set; }
}

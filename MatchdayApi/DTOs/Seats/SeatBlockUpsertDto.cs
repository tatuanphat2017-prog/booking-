using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Seats;

public class SeatBlockUpsertDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100000000)]
    public decimal BasePrice { get; set; }

    public int StadiumId { get; set; }
}

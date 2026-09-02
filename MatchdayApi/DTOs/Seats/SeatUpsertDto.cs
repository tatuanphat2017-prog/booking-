using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Seats;

public class SeatUpsertDto
{
    [Required, MaxLength(5)]
    public string RowLabel { get; set; } = string.Empty;

    [Range(1, 999)]
    public int SeatNumber { get; set; }

    public int SeatBlockId { get; set; }
}

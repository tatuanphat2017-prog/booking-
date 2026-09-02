using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Bookings;

public class CreateBookingDto
{
    public int MatchId { get; set; }

    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 ghế.")]
    public List<int> SeatIds { get; set; } = new();
}

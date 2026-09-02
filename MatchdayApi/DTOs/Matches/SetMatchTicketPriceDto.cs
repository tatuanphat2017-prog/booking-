using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Matches;

public class SetMatchTicketPriceDto
{
    public int SeatBlockId { get; set; }

    [Range(0, 100000000)]
    public decimal Price { get; set; }
}

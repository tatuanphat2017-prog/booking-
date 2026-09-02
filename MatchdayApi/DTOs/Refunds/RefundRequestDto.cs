using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Refunds;

public class RefundRequestDto
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

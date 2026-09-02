using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Stadiums;

public class StadiumUpsertDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(1, 300000)]
    public int Capacity { get; set; }
}

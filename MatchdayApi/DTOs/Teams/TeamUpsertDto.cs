using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Teams;

public class TeamUpsertDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string ShortName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    public int? HomeStadiumId { get; set; }
}

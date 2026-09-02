using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Players;

public class PlayerUpsertDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Range(0, 99)]
    public int JerseyNumber { get; set; }

    [Required, MaxLength(10)]
    public string Position { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    public int TeamId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Matches;

public class SetPlayerMatchStatDto
{
    [Required]
    public int PlayerId { get; set; }

    [Range(0, 20)] public int Goals { get; set; }
    [Range(0, 20)] public int Assists { get; set; }
    [Range(0, 5)] public int YellowCards { get; set; }
    [Range(0, 2)] public int RedCards { get; set; }
    [Range(0, 120)] public int MinutesPlayed { get; set; }
    [Range(0, 10)] public decimal Rating { get; set; }
}

public class SetPlayerMatchStatsDto
{
    [Required]
    public List<SetPlayerMatchStatDto> Stats { get; set; } = new();
}

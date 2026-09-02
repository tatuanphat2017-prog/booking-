using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Matches;

public class SetLineupPlayerDto
{
    [Required]
    public int PlayerId { get; set; }

    [Range(0, 100)]
    public double PositionX { get; set; }

    [Range(0, 100)]
    public double PositionY { get; set; }

    public bool IsStarting { get; set; } = true;
}

public class SetLineupDto
{
    [Required, MaxLength(20)]
    public string Formation { get; set; } = string.Empty; // vd: 4-3-3

    [Required]
    public List<SetLineupPlayerDto> Players { get; set; } = new();
}

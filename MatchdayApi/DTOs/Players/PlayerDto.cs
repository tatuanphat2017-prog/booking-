namespace MatchdayApi.DTOs.Players;

public class PlayerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int JerseyNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? PhotoUrl { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
}

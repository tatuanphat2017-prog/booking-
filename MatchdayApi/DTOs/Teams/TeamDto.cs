namespace MatchdayApi.DTOs.Teams;

public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int? HomeStadiumId { get; set; }
    public string? HomeStadiumName { get; set; }
}

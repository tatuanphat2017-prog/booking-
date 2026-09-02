namespace MatchdayApi.DTOs.Dashboard;

/// <summary>Thẻ trận đấu hiện ở Dashboard cho trận Đã kết thúc, kèm tỷ số.</summary>
public class FinishedMatchDto
{
    public int Id { get; set; }
    public DateTime MatchDateTime { get; set; }

    public string HomeTeamName { get; set; } = string.Empty;
    public string? HomeTeamLogoUrl { get; set; }

    public string AwayTeamName { get; set; } = string.Empty;
    public string? AwayTeamLogoUrl { get; set; }

    public string StadiumName { get; set; } = string.Empty;

    /// <summary>Tỷ số — null nếu trận đã đánh dấu Finished nhưng Admin chưa nhập kết quả.</summary>
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}

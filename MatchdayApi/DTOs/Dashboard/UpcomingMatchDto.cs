using MatchdayApi.Enums;

namespace MatchdayApi.DTOs.Dashboard;

/// <summary>Thẻ trận đấu hiện ở Dashboard cho trận Sắp diễn ra / Đang diễn ra.</summary>
public class UpcomingMatchDto
{
    public int Id { get; set; }
    public DateTime MatchDateTime { get; set; }
    public MatchStatus Status { get; set; }

    public string HomeTeamName { get; set; } = string.Empty;
    public string? HomeTeamLogoUrl { get; set; }

    public string AwayTeamName { get; set; } = string.Empty;
    public string? AwayTeamLogoUrl { get; set; }

    public string StadiumName { get; set; } = string.Empty;
    public string StadiumCity { get; set; } = string.Empty;

    /// <summary>Giá vé thấp nhất trong các khối ghế của trận (null nếu Admin chưa đặt giá).</summary>
    public decimal? MinTicketPrice { get; set; }

    /// <summary>Giá vé cao nhất trong các khối ghế của trận.</summary>
    public decimal? MaxTicketPrice { get; set; }
}

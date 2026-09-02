using MatchdayApi.DTOs.Matches;
using MatchdayApi.Enums;

namespace MatchdayApi.DTOs.Dashboard;

/// <summary>Chi tiết 1 trận đấu khi user bấm vào từ Dashboard — gộp thông tin trận + giá vé (nếu chưa đá) + kết quả (nếu đã đá).</summary>
public class MatchDetailDto
{
    public int Id { get; set; }
    public DateTime MatchDateTime { get; set; }
    public MatchStatus Status { get; set; }

    public int HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public string? HomeTeamLogoUrl { get; set; }

    public int AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = string.Empty;
    public string? AwayTeamLogoUrl { get; set; }

    public int StadiumId { get; set; }
    public string StadiumName { get; set; } = string.Empty;
    public string StadiumAddress { get; set; } = string.Empty;
    public string StadiumCity { get; set; } = string.Empty;

    /// <summary>Giá vé theo từng khối ghế — chỉ có ý nghĩa khi trận chưa kết thúc (còn bán vé).</summary>
    public List<MatchTicketPriceItemDto> TicketPrices { get; set; } = new();

    /// <summary>Kết quả trận đấu — null nếu trận chưa Finished hoặc Admin chưa nhập kết quả.</summary>
    public MatchResultDto? Result { get; set; }

    /// <summary>Đội hình ra sân đội nhà — null nếu Admin chưa xếp (task #20).</summary>
    public TeamLineupDto? HomeLineup { get; set; }

    /// <summary>Đội hình ra sân đội khách — null nếu Admin chưa xếp (task #20).</summary>
    public TeamLineupDto? AwayLineup { get; set; }

    /// <summary>Chỉ số các cầu thủ đã thi đấu trong trận — rỗng nếu Admin chưa nhập (task #20).</summary>
    public List<PlayerMatchStatDto> PlayerStats { get; set; } = new();
}

public class MatchTicketPriceItemDto
{
    public int SeatBlockId { get; set; }
    public string SeatBlockName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

using MatchdayApi.Data;
using MatchdayApi.DTOs.Dashboard;
using MatchdayApi.DTOs.Matches;
using MatchdayApi.Enums;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

/// <summary>
/// API riêng cho màn Dashboard (trang chủ) phía User — task #11.
/// Khác với /api/Matches (dùng cho CRUD Admin), Controller này trả về DTO đã gộp sẵn
/// thông tin cần hiển thị trực tiếp lên UI (logo, giá vé thấp nhất/cao nhất, tỷ số...),
/// giúp frontend không phải gọi nhiều API rồi tự ráp dữ liệu.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Trận sắp diễn ra + đang diễn ra, sắp xếp gần nhất lên trước — công khai.
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<List<UpcomingMatchDto>>> GetUpcoming([FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 100);

        var matches = await _db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Stadium)
            .Include(m => m.TicketPrices)
            .Where(m => m.Status == MatchStatus.Upcoming || m.Status == MatchStatus.Live)
            .OrderBy(m => m.MatchDateTime)
            .Take(take)
            .ToListAsync();

        var result = matches.Select(m => new UpcomingMatchDto
        {
            Id = m.Id,
            MatchDateTime = m.MatchDateTime,
            Status = m.Status,
            HomeTeamName = m.HomeTeam.Name,
            HomeTeamLogoUrl = m.HomeTeam.LogoUrl,
            AwayTeamName = m.AwayTeam.Name,
            AwayTeamLogoUrl = m.AwayTeam.LogoUrl,
            StadiumName = m.Stadium.Name,
            StadiumCity = m.Stadium.City,
            MinTicketPrice = m.TicketPrices.Count > 0 ? m.TicketPrices.Min(p => p.Price) : null,
            MaxTicketPrice = m.TicketPrices.Count > 0 ? m.TicketPrices.Max(p => p.Price) : null
        });

        return Ok(result);
    }

    /// <summary>
    /// Trận đã kết thúc, sắp xếp mới đá gần đây nhất lên trước — công khai.
    /// </summary>
    [HttpGet("finished")]
    public async Task<ActionResult<List<FinishedMatchDto>>> GetFinished([FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 100);

        var matches = await _db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Stadium)
            .Include(m => m.Result)
            .Where(m => m.Status == MatchStatus.Finished)
            .OrderByDescending(m => m.MatchDateTime)
            .Take(take)
            .ToListAsync();

        var result = matches.Select(m => new FinishedMatchDto
        {
            Id = m.Id,
            MatchDateTime = m.MatchDateTime,
            HomeTeamName = m.HomeTeam.Name,
            HomeTeamLogoUrl = m.HomeTeam.LogoUrl,
            AwayTeamName = m.AwayTeam.Name,
            AwayTeamLogoUrl = m.AwayTeam.LogoUrl,
            StadiumName = m.Stadium.Name,
            HomeScore = m.Result?.HomeScore,
            AwayScore = m.Result?.AwayScore
        });

        return Ok(result);
    }

    /// <summary>
    /// Chi tiết 1 trận — dùng khi user bấm vào 1 thẻ trận từ Dashboard. Công khai.
    /// Trả kèm giá vé theo khối ghế (nếu còn bán) hoặc kết quả trận đấu (nếu đã đá xong).
    /// </summary>
    [HttpGet("matches/{id:int}")]
    public async Task<ActionResult<MatchDetailDto>> GetMatchDetail(int id)
    {
        var match = await _db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Stadium)
            .Include(m => m.Result)
            .Include(m => m.TicketPrices).ThenInclude(p => p.SeatBlock)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match is null) return NotFound(new { message = "Không tìm thấy trận đấu." });

        // Đội hình + chỉ số cầu thủ (task #20) — gộp sẵn vào đây để user xem chi tiết trận
        // không phải gọi thêm API riêng của /api/Matches.
        var lineups = await _db.MatchTeamLineups
            .Include(l => l.Team)
            .Include(l => l.Players).ThenInclude(lp => lp.Player)
            .Where(l => l.MatchId == id)
            .ToListAsync();

        var homeLineup = lineups.FirstOrDefault(l => l.TeamId == match.HomeTeamId);
        var awayLineup = lineups.FirstOrDefault(l => l.TeamId == match.AwayTeamId);

        var playerStats = await _db.PlayerMatchStats
            .Include(s => s.Player).ThenInclude(p => p.Team)
            .Where(s => s.MatchId == id)
            .OrderByDescending(s => s.Goals)
            .ToListAsync();

        var dto = new MatchDetailDto
        {
            Id = match.Id,
            MatchDateTime = match.MatchDateTime,
            Status = match.Status,
            HomeTeamId = match.HomeTeamId,
            HomeTeamName = match.HomeTeam.Name,
            HomeTeamLogoUrl = match.HomeTeam.LogoUrl,
            AwayTeamId = match.AwayTeamId,
            AwayTeamName = match.AwayTeam.Name,
            AwayTeamLogoUrl = match.AwayTeam.LogoUrl,
            StadiumId = match.StadiumId,
            StadiumName = match.Stadium.Name,
            StadiumAddress = match.Stadium.Address,
            StadiumCity = match.Stadium.City,
            TicketPrices = match.TicketPrices.Select(p => new MatchTicketPriceItemDto
            {
                SeatBlockId = p.SeatBlockId,
                SeatBlockName = p.SeatBlock.Name,
                Price = p.Price
            }).ToList(),
            Result = match.Result is null ? null : new MatchResultDto
            {
                HomeScore = match.Result.HomeScore,
                AwayScore = match.Result.AwayScore,
                PossessionHome = match.Result.PossessionHome,
                PossessionAway = match.Result.PossessionAway,
                ShotsHome = match.Result.ShotsHome,
                ShotsAway = match.Result.ShotsAway,
                YellowCardsHome = match.Result.YellowCardsHome,
                YellowCardsAway = match.Result.YellowCardsAway,
                RedCardsHome = match.Result.RedCardsHome,
                RedCardsAway = match.Result.RedCardsAway,
                CornersHome = match.Result.CornersHome,
                CornersAway = match.Result.CornersAway
            },
            HomeLineup = homeLineup is null ? null : ToTeamLineupDto(homeLineup),
            AwayLineup = awayLineup is null ? null : ToTeamLineupDto(awayLineup),
            PlayerStats = playerStats.Select(ToPlayerStatDto).ToList()
        };

        return Ok(dto);
    }

    private static TeamLineupDto ToTeamLineupDto(MatchTeamLineup l) => new()
    {
        TeamId = l.TeamId,
        TeamName = l.Team?.Name ?? string.Empty,
        Formation = l.Formation,
        Players = l.Players.Select(lp => new LineupPlayerDto
        {
            PlayerId = lp.PlayerId,
            PlayerName = lp.Player?.FullName ?? string.Empty,
            JerseyNumber = lp.Player?.JerseyNumber ?? 0,
            Position = lp.Player?.Position ?? string.Empty,
            PositionX = lp.PositionX,
            PositionY = lp.PositionY,
            IsStarting = lp.IsStarting
        }).OrderByDescending(p => p.IsStarting).ThenBy(p => p.JerseyNumber).ToList()
    };

    private static PlayerMatchStatDto ToPlayerStatDto(PlayerMatchStat s) => new()
    {
        PlayerId = s.PlayerId,
        PlayerName = s.Player?.FullName ?? string.Empty,
        JerseyNumber = s.Player?.JerseyNumber ?? 0,
        TeamId = s.Player?.TeamId ?? 0,
        TeamName = s.Player?.Team?.Name ?? string.Empty,
        Goals = s.Goals,
        Assists = s.Assists,
        YellowCards = s.YellowCards,
        RedCards = s.RedCards,
        MinutesPlayed = s.MinutesPlayed,
        Rating = s.Rating
    };
}

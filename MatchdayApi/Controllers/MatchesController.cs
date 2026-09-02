using System.Security.Claims;
using MatchdayApi.Data;
using MatchdayApi.DTOs.Bookings;
using MatchdayApi.DTOs.Matches;
using MatchdayApi.Enums;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MatchesController(AppDbContext db)
    {
        _db = db;
    }

    private static MatchDto ToDto(Match m) => new()
    {
        Id = m.Id,
        MatchDateTime = m.MatchDateTime,
        Status = m.Status,
        HomeTeamId = m.HomeTeamId,
        HomeTeamName = m.HomeTeam?.Name ?? string.Empty,
        AwayTeamId = m.AwayTeamId,
        AwayTeamName = m.AwayTeam?.Name ?? string.Empty,
        StadiumId = m.StadiumId,
        StadiumName = m.Stadium?.Name ?? string.Empty
    };

    /// <summary>Danh sách trận đấu — công khai. Có thể lọc theo trạng thái bằng ?status=Upcoming|Live|Finished</summary>
    [HttpGet]
    public async Task<ActionResult<List<MatchDto>>> GetAll([FromQuery] MatchStatus? status)
    {
        var query = _db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Stadium)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status);
        }

        var matches = await query.OrderBy(m => m.MatchDateTime).ToListAsync();
        return Ok(matches.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MatchDto>> GetById(int id)
    {
        var match = await _db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Stadium)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match is null) return NotFound(new { message = "Không tìm thấy trận đấu." });

        return Ok(ToDto(match));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<MatchDto>> Create(MatchUpsertDto dto)
    {
        var error = await ValidateTeamsAndStadium(dto);
        if (error is not null) return error;

        var match = new Match
        {
            MatchDateTime = dto.MatchDateTime,
            Status = dto.Status,
            HomeTeamId = dto.HomeTeamId,
            AwayTeamId = dto.AwayTeamId,
            StadiumId = dto.StadiumId
        };

        _db.Matches.Add(match);
        await _db.SaveChangesAsync();
        await _db.Entry(match).Reference(m => m.HomeTeam).LoadAsync();
        await _db.Entry(match).Reference(m => m.AwayTeam).LoadAsync();
        await _db.Entry(match).Reference(m => m.Stadium).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = match.Id }, ToDto(match));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MatchUpsertDto dto)
    {
        var match = await _db.Matches.FindAsync(id);
        if (match is null) return NotFound(new { message = "Không tìm thấy trận đấu." });

        var error = await ValidateTeamsAndStadium(dto);
        if (error is not null) return error;

        match.MatchDateTime = dto.MatchDateTime;
        match.Status = dto.Status;
        match.HomeTeamId = dto.HomeTeamId;
        match.AwayTeamId = dto.AwayTeamId;
        match.StadiumId = dto.StadiumId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var match = await _db.Matches.FindAsync(id);
        if (match is null) return NotFound(new { message = "Không tìm thấy trận đấu." });

        _db.Matches.Remove(match);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Không thể xóa vì trận đấu này đã có vé đặt/thanh toán liên quan." });
        }

        return NoContent();
    }

    // ---------- Giá vé theo khối ghế cho từng trận ----------

    /// <summary>Danh sách giá vé theo khối ghế của 1 trận — công khai (cần để user xem giá trước khi đặt).</summary>
    [HttpGet("{matchId:int}/ticket-prices")]
    public async Task<ActionResult<List<MatchTicketPriceDto>>> GetTicketPrices(int matchId)
    {
        if (!await _db.Matches.AnyAsync(m => m.Id == matchId))
        {
            return NotFound(new { message = "Không tìm thấy trận đấu." });
        }

        var prices = await _db.MatchTicketPrices
            .Include(p => p.SeatBlock)
            .Where(p => p.MatchId == matchId)
            .ToListAsync();

        return Ok(prices.Select(p => new MatchTicketPriceDto
        {
            Id = p.Id,
            MatchId = p.MatchId,
            SeatBlockId = p.SeatBlockId,
            SeatBlockName = p.SeatBlock.Name,
            Price = p.Price
        }));
    }

    /// <summary>Đặt/cập nhật giá vé cho 1 khối ghế của trận (Admin). Nếu chưa có thì tạo mới, có rồi thì cập nhật giá.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{matchId:int}/ticket-prices")]
    public async Task<ActionResult<MatchTicketPriceDto>> SetTicketPrice(int matchId, SetMatchTicketPriceDto dto)
    {
        if (!await _db.Matches.AnyAsync(m => m.Id == matchId))
        {
            return NotFound(new { message = "Không tìm thấy trận đấu." });
        }

        if (!await _db.SeatBlocks.AnyAsync(sb => sb.Id == dto.SeatBlockId))
        {
            return BadRequest(new { message = "SeatBlockId không tồn tại." });
        }

        var existing = await _db.MatchTicketPrices
            .Include(p => p.SeatBlock)
            .FirstOrDefaultAsync(p => p.MatchId == matchId && p.SeatBlockId == dto.SeatBlockId);

        if (existing is not null)
        {
            existing.Price = dto.Price;
            await _db.SaveChangesAsync();
            return Ok(new MatchTicketPriceDto
            {
                Id = existing.Id,
                MatchId = existing.MatchId,
                SeatBlockId = existing.SeatBlockId,
                SeatBlockName = existing.SeatBlock.Name,
                Price = existing.Price
            });
        }

        var newPrice = new MatchTicketPrice
        {
            MatchId = matchId,
            SeatBlockId = dto.SeatBlockId,
            Price = dto.Price
        };
        _db.MatchTicketPrices.Add(newPrice);
        await _db.SaveChangesAsync();
        await _db.Entry(newPrice).Reference(p => p.SeatBlock).LoadAsync();

        return Ok(new MatchTicketPriceDto
        {
            Id = newPrice.Id,
            MatchId = newPrice.MatchId,
            SeatBlockId = newPrice.SeatBlockId,
            SeatBlockName = newPrice.SeatBlock.Name,
            Price = newPrice.Price
        });
    }

    // ---------- Sơ đồ ghế theo trạng thái (task #12 — chọn ghế real-time) ----------

    /// <summary>
    /// Sơ đồ toàn bộ ghế của sân thi đấu cho 1 trận cụ thể, kèm trạng thái Available/Held/Booked
    /// và giá vé theo khối ghế. Công khai — không cần đăng nhập để xem, nhưng nếu có đăng nhập
    /// (gửi kèm Bearer token) thì sẽ biết ghế nào đang do CHÍNH mình giữ (IsHeldByMe).
    /// Dùng để frontend vẽ sơ đồ ghế ban đầu, sau đó lắng nghe SignalR (Hub SeatSelectionHub)
    /// để cập nhật real-time khi có người khác chọn/nhả/đặt ghế.
    /// </summary>
    [HttpGet("{matchId:int}/seat-map")]
    public async Task<ActionResult<List<SeatMapItemDto>>> GetSeatMap(int matchId)
    {
        var match = await _db.Matches.FindAsync(matchId);
        if (match is null) return NotFound(new { message = "Không tìm thấy trận đấu." });

        var seats = await _db.Seats
            .Include(s => s.SeatBlock)
            .Where(s => s.SeatBlock.StadiumId == match.StadiumId)
            .ToListAsync();

        var seatIds = seats.Select(s => s.Id).ToList();

        var prices = await _db.MatchTicketPrices
            .Where(p => p.MatchId == matchId)
            .ToDictionaryAsync(p => p.SeatBlockId, p => p.Price);

        var bookedSeatIds = (await _db.BookingSeats
                .Where(bs => bs.MatchId == matchId)
                .Select(bs => bs.SeatId)
                .ToListAsync())
            .ToHashSet();

        var now = DateTime.UtcNow;
        var holds = await _db.SeatHolds
            .Where(h => h.MatchId == matchId && h.ExpireAt > now)
            .ToListAsync();
        var holdMap = holds.ToDictionary(h => h.SeatId, h => h.UserId);

        int? currentUserId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (idClaim is not null) currentUserId = int.Parse(idClaim);
        }

        var result = seats
            .OrderBy(s => s.SeatBlockId).ThenBy(s => s.RowLabel).ThenBy(s => s.SeatNumber)
            .Select(s =>
            {
                var isBooked = bookedSeatIds.Contains(s.Id);
                var isHeld = !isBooked && holdMap.ContainsKey(s.Id);

                return new SeatMapItemDto
                {
                    SeatId = s.Id,
                    RowLabel = s.RowLabel,
                    SeatNumber = s.SeatNumber,
                    SeatBlockId = s.SeatBlockId,
                    SeatBlockName = s.SeatBlock.Name,
                    Price = prices.TryGetValue(s.SeatBlockId, out var price) ? price : null,
                    Status = isBooked ? "Booked" : (isHeld ? "Held" : "Available"),
                    IsHeldByMe = isHeld && currentUserId.HasValue && holdMap[s.Id] == currentUserId.Value
                };
            })
            .ToList();

        return Ok(result);
    }

    // ---------- Đội hình ra sân (task #20) ----------

    /// <summary>Đội hình ra sân của 2 đội trong 1 trận — công khai. Đội nào chưa xếp đội hình thì trả về null.</summary>
    [HttpGet("{matchId:int}/lineup")]
    public async Task<ActionResult<MatchLineupDto>> GetLineup(int matchId)
    {
        var match = await _db.Matches.FindAsync(matchId);
        if (match is null) return NotFound(new { message = "Không tìm thấy trận đấu." });

        var lineups = await _db.MatchTeamLineups
            .Include(l => l.Team)
            .Include(l => l.Players).ThenInclude(lp => lp.Player)
            .Where(l => l.MatchId == matchId)
            .ToListAsync();

        var homeLineup = lineups.FirstOrDefault(l => l.TeamId == match.HomeTeamId);
        var awayLineup = lineups.FirstOrDefault(l => l.TeamId == match.AwayTeamId);

        return Ok(new MatchLineupDto
        {
            MatchId = matchId,
            Home = homeLineup is null ? null : ToTeamLineupDto(homeLineup),
            Away = awayLineup is null ? null : ToTeamLineupDto(awayLineup)
        });
    }

    /// <summary>
    /// Xếp/cập nhật đội hình ra sân cho 1 đội trong 1 trận (Admin). teamId phải là đội nhà hoặc đội
    /// khách của trận đó. Nếu đội này đã có đội hình từ trước thì thay thế toàn bộ (xóa danh sách cũ,
    /// ghi danh sách mới) — không cộng dồn.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{matchId:int}/lineup/{teamId:int}")]
    public async Task<ActionResult<TeamLineupDto>> SetLineup(int matchId, int teamId, SetLineupDto dto)
    {
        var match = await _db.Matches.FindAsync(matchId);
        if (match is null) return NotFound(new { message = "Không tìm thấy trận đấu." });

        if (teamId != match.HomeTeamId && teamId != match.AwayTeamId)
        {
            return BadRequest(new { message = "Đội này không thi đấu trong trận đã chọn." });
        }

        if (dto.Players.Count == 0)
        {
            return BadRequest(new { message = "Đội hình phải có ít nhất 1 cầu thủ." });
        }

        var playerIds = dto.Players.Select(p => p.PlayerId).ToList();
        if (playerIds.Distinct().Count() != playerIds.Count)
        {
            return BadRequest(new { message = "Danh sách cầu thủ bị trùng." });
        }

        var validPlayerIds = await _db.Players
            .Where(p => p.TeamId == teamId && playerIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var invalidIds = playerIds.Except(validPlayerIds).ToList();
        if (invalidIds.Count > 0)
        {
            return BadRequest(new { message = $"Cầu thủ không thuộc đội này hoặc không tồn tại (Id: {string.Join(", ", invalidIds)})." });
        }

        var startingCount = dto.Players.Count(p => p.IsStarting);
        if (startingCount > 11)
        {
            return BadRequest(new { message = "Số cầu thủ đá chính không được vượt quá 11." });
        }

        var lineup = await _db.MatchTeamLineups
            .Include(l => l.Players)
            .FirstOrDefaultAsync(l => l.MatchId == matchId && l.TeamId == teamId);

        if (lineup is null)
        {
            lineup = new MatchTeamLineup { MatchId = matchId, TeamId = teamId, Formation = dto.Formation };
            _db.MatchTeamLineups.Add(lineup);
        }
        else
        {
            lineup.Formation = dto.Formation;
            _db.LineupPlayers.RemoveRange(lineup.Players);
            lineup.Players.Clear();
        }

        foreach (var p in dto.Players)
        {
            lineup.Players.Add(new LineupPlayer
            {
                PlayerId = p.PlayerId,
                PositionX = p.PositionX,
                PositionY = p.PositionY,
                IsStarting = p.IsStarting
            });
        }

        await _db.SaveChangesAsync();

        await _db.Entry(lineup).Reference(l => l.Team).LoadAsync();
        foreach (var lp in lineup.Players)
        {
            await _db.Entry(lp).Reference(x => x.Player).LoadAsync();
        }

        return Ok(ToTeamLineupDto(lineup));
    }

    // ---------- Kết quả trận đấu (task #20) ----------

    /// <summary>Kết quả trận đấu — công khai. Trả 404 nếu trận chưa có kết quả (chưa đá xong).</summary>
    [HttpGet("{matchId:int}/result")]
    public async Task<ActionResult<MatchFullResultDto>> GetResult(int matchId)
    {
        if (!await _db.Matches.AnyAsync(m => m.Id == matchId))
        {
            return NotFound(new { message = "Không tìm thấy trận đấu." });
        }

        var result = await _db.MatchResults.FirstOrDefaultAsync(r => r.MatchId == matchId);
        if (result is null) return NotFound(new { message = "Trận đấu này chưa có kết quả." });

        return Ok(ToResultDto(result));
    }

    /// <summary>Nhập/cập nhật kết quả + thống kê trận đấu (Admin). Gọi lại nhiều lần sẽ ghi đè kết quả cũ.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{matchId:int}/result")]
    public async Task<ActionResult<MatchFullResultDto>> SetResult(int matchId, SetMatchResultDto dto)
    {
        if (!await _db.Matches.AnyAsync(m => m.Id == matchId))
        {
            return NotFound(new { message = "Không tìm thấy trận đấu." });
        }

        var result = await _db.MatchResults.FirstOrDefaultAsync(r => r.MatchId == matchId);
        if (result is null)
        {
            result = new MatchResult { MatchId = matchId };
            _db.MatchResults.Add(result);
        }

        result.HomeScore = dto.HomeScore;
        result.AwayScore = dto.AwayScore;
        result.PossessionHome = dto.PossessionHome;
        result.PossessionAway = dto.PossessionAway;
        result.ShotsHome = dto.ShotsHome;
        result.ShotsAway = dto.ShotsAway;
        result.YellowCardsHome = dto.YellowCardsHome;
        result.YellowCardsAway = dto.YellowCardsAway;
        result.RedCardsHome = dto.RedCardsHome;
        result.RedCardsAway = dto.RedCardsAway;
        result.CornersHome = dto.CornersHome;
        result.CornersAway = dto.CornersAway;

        await _db.SaveChangesAsync();

        return Ok(ToResultDto(result));
    }

    // ---------- Chỉ số cầu thủ theo trận (task #20) ----------

    /// <summary>Chỉ số các cầu thủ đã thi đấu trong trận — công khai. Lọc theo đội bằng ?teamId=.</summary>
    [HttpGet("{matchId:int}/player-stats")]
    public async Task<ActionResult<List<PlayerMatchStatDto>>> GetPlayerStats(int matchId, [FromQuery] int? teamId)
    {
        if (!await _db.Matches.AnyAsync(m => m.Id == matchId))
        {
            return NotFound(new { message = "Không tìm thấy trận đấu." });
        }

        var query = _db.PlayerMatchStats
            .Include(s => s.Player).ThenInclude(p => p.Team)
            .Where(s => s.MatchId == matchId);

        if (teamId.HasValue)
        {
            query = query.Where(s => s.Player.TeamId == teamId);
        }

        var stats = await query
            .OrderByDescending(s => s.Goals)
            .ToListAsync();

        return Ok(stats.Select(ToPlayerStatDto));
    }

    /// <summary>
    /// Nhập/cập nhật chỉ số cầu thủ cho trận (Admin) — gửi kèm danh sách, mỗi cầu thủ 1 dòng.
    /// Cầu thủ đã có chỉ số ở trận này thì được ghi đè, chưa có thì tạo mới.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{matchId:int}/player-stats")]
    public async Task<ActionResult<List<PlayerMatchStatDto>>> SetPlayerStats(int matchId, SetPlayerMatchStatsDto dto)
    {
        if (!await _db.Matches.AnyAsync(m => m.Id == matchId))
        {
            return NotFound(new { message = "Không tìm thấy trận đấu." });
        }

        if (dto.Stats.Count == 0)
        {
            return BadRequest(new { message = "Danh sách chỉ số không được để trống." });
        }

        var playerIds = dto.Stats.Select(s => s.PlayerId).ToList();
        if (playerIds.Distinct().Count() != playerIds.Count)
        {
            return BadRequest(new { message = "Danh sách cầu thủ bị trùng." });
        }

        var validPlayerIds = await _db.Players
            .Where(p => playerIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var invalidIds = playerIds.Except(validPlayerIds).ToList();
        if (invalidIds.Count > 0)
        {
            return BadRequest(new { message = $"Cầu thủ không tồn tại (Id: {string.Join(", ", invalidIds)})." });
        }

        var existingStats = await _db.PlayerMatchStats
            .Where(s => s.MatchId == matchId && playerIds.Contains(s.PlayerId))
            .ToListAsync();

        foreach (var item in dto.Stats)
        {
            var existing = existingStats.FirstOrDefault(s => s.PlayerId == item.PlayerId);
            if (existing is null)
            {
                _db.PlayerMatchStats.Add(new PlayerMatchStat
                {
                    MatchId = matchId,
                    PlayerId = item.PlayerId,
                    Goals = item.Goals,
                    Assists = item.Assists,
                    YellowCards = item.YellowCards,
                    RedCards = item.RedCards,
                    MinutesPlayed = item.MinutesPlayed,
                    Rating = item.Rating
                });
            }
            else
            {
                existing.Goals = item.Goals;
                existing.Assists = item.Assists;
                existing.YellowCards = item.YellowCards;
                existing.RedCards = item.RedCards;
                existing.MinutesPlayed = item.MinutesPlayed;
                existing.Rating = item.Rating;
            }
        }

        await _db.SaveChangesAsync();

        var savedStats = await _db.PlayerMatchStats
            .Include(s => s.Player).ThenInclude(p => p.Team)
            .Where(s => s.MatchId == matchId && playerIds.Contains(s.PlayerId))
            .ToListAsync();

        return Ok(savedStats.Select(ToPlayerStatDto));
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

    private static MatchFullResultDto ToResultDto(MatchResult r) => new()
    {
        MatchId = r.MatchId,
        HomeScore = r.HomeScore,
        AwayScore = r.AwayScore,
        PossessionHome = r.PossessionHome,
        PossessionAway = r.PossessionAway,
        ShotsHome = r.ShotsHome,
        ShotsAway = r.ShotsAway,
        YellowCardsHome = r.YellowCardsHome,
        YellowCardsAway = r.YellowCardsAway,
        RedCardsHome = r.RedCardsHome,
        RedCardsAway = r.RedCardsAway,
        CornersHome = r.CornersHome,
        CornersAway = r.CornersAway
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

    private async Task<ActionResult?> ValidateTeamsAndStadium(MatchUpsertDto dto)
    {
        if (dto.HomeTeamId == dto.AwayTeamId)
        {
            return BadRequest(new { message = "Đội nhà và đội khách không được trùng nhau." });
        }

        if (!await _db.Teams.AnyAsync(t => t.Id == dto.HomeTeamId))
        {
            return BadRequest(new { message = "HomeTeamId không tồn tại." });
        }

        if (!await _db.Teams.AnyAsync(t => t.Id == dto.AwayTeamId))
        {
            return BadRequest(new { message = "AwayTeamId không tồn tại." });
        }

        if (!await _db.Stadiums.AnyAsync(s => s.Id == dto.StadiumId))
        {
            return BadRequest(new { message = "StadiumId không tồn tại." });
        }

        return null;
    }
}

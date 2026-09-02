using MatchdayApi.Data;
using MatchdayApi.DTOs.Players;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlayersController(AppDbContext db)
    {
        _db = db;
    }

    private static PlayerDto ToDto(Player p) => new()
    {
        Id = p.Id,
        FullName = p.FullName,
        JerseyNumber = p.JerseyNumber,
        Position = p.Position,
        DateOfBirth = p.DateOfBirth,
        PhotoUrl = p.PhotoUrl,
        TeamId = p.TeamId,
        TeamName = p.Team?.Name ?? string.Empty
    };

    /// <summary>Danh sách cầu thủ — công khai. Có thể lọc theo đội bằng ?teamId=</summary>
    [HttpGet]
    public async Task<ActionResult<List<PlayerDto>>> GetAll([FromQuery] int? teamId)
    {
        var query = _db.Players.Include(p => p.Team).AsQueryable();
        if (teamId.HasValue)
        {
            query = query.Where(p => p.TeamId == teamId);
        }

        var players = await query
            .OrderBy(p => p.TeamId).ThenBy(p => p.JerseyNumber)
            .ToListAsync();

        return Ok(players.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlayerDto>> GetById(int id)
    {
        var player = await _db.Players.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == id);
        if (player is null) return NotFound(new { message = "Không tìm thấy cầu thủ." });

        return Ok(ToDto(player));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<PlayerDto>> Create(PlayerUpsertDto dto)
    {
        if (!await _db.Teams.AnyAsync(t => t.Id == dto.TeamId))
        {
            return BadRequest(new { message = "TeamId không tồn tại." });
        }

        var player = new Player
        {
            FullName = dto.FullName,
            JerseyNumber = dto.JerseyNumber,
            Position = dto.Position,
            DateOfBirth = dto.DateOfBirth,
            PhotoUrl = dto.PhotoUrl,
            TeamId = dto.TeamId
        };

        _db.Players.Add(player);
        await _db.SaveChangesAsync();
        await _db.Entry(player).Reference(p => p.Team).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = player.Id }, ToDto(player));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PlayerUpsertDto dto)
    {
        var player = await _db.Players.FindAsync(id);
        if (player is null) return NotFound(new { message = "Không tìm thấy cầu thủ." });

        if (!await _db.Teams.AnyAsync(t => t.Id == dto.TeamId))
        {
            return BadRequest(new { message = "TeamId không tồn tại." });
        }

        player.FullName = dto.FullName;
        player.JerseyNumber = dto.JerseyNumber;
        player.Position = dto.Position;
        player.DateOfBirth = dto.DateOfBirth;
        player.PhotoUrl = dto.PhotoUrl;
        player.TeamId = dto.TeamId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var player = await _db.Players.FindAsync(id);
        if (player is null) return NotFound(new { message = "Không tìm thấy cầu thủ." });

        _db.Players.Remove(player);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Không thể xóa vì cầu thủ này đang có dữ liệu đội hình/chỉ số trận đấu liên quan." });
        }

        return NoContent();
    }
}

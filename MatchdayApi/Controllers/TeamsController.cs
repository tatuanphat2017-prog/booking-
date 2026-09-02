using MatchdayApi.Data;
using MatchdayApi.DTOs.Teams;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeamsController(AppDbContext db)
    {
        _db = db;
    }

    private static TeamDto ToDto(Team t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        ShortName = t.ShortName,
        LogoUrl = t.LogoUrl,
        HomeStadiumId = t.HomeStadiumId,
        HomeStadiumName = t.HomeStadium?.Name
    };

    /// <summary>Danh sách đội bóng — công khai.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TeamDto>>> GetAll()
    {
        var teams = await _db.Teams
            .Include(t => t.HomeStadium)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return Ok(teams.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeamDto>> GetById(int id)
    {
        var team = await _db.Teams.Include(t => t.HomeStadium).FirstOrDefaultAsync(t => t.Id == id);
        if (team is null) return NotFound(new { message = "Không tìm thấy đội bóng." });

        return Ok(ToDto(team));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create(TeamUpsertDto dto)
    {
        if (dto.HomeStadiumId.HasValue && !await _db.Stadiums.AnyAsync(s => s.Id == dto.HomeStadiumId))
        {
            return BadRequest(new { message = "HomeStadiumId không tồn tại." });
        }

        var team = new Team
        {
            Name = dto.Name,
            ShortName = dto.ShortName,
            LogoUrl = dto.LogoUrl,
            HomeStadiumId = dto.HomeStadiumId
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync();
        await _db.Entry(team).Reference(t => t.HomeStadium).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = team.Id }, ToDto(team));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TeamUpsertDto dto)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team is null) return NotFound(new { message = "Không tìm thấy đội bóng." });

        if (dto.HomeStadiumId.HasValue && !await _db.Stadiums.AnyAsync(s => s.Id == dto.HomeStadiumId))
        {
            return BadRequest(new { message = "HomeStadiumId không tồn tại." });
        }

        team.Name = dto.Name;
        team.ShortName = dto.ShortName;
        team.LogoUrl = dto.LogoUrl;
        team.HomeStadiumId = dto.HomeStadiumId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team is null) return NotFound(new { message = "Không tìm thấy đội bóng." });

        _db.Teams.Remove(team);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Không thể xóa vì đội bóng này đang có cầu thủ hoặc trận đấu liên quan." });
        }

        return NoContent();
    }
}

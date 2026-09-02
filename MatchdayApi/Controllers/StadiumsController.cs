using MatchdayApi.Data;
using MatchdayApi.DTOs.Stadiums;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StadiumsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StadiumsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Danh sách sân vận động — công khai, không cần đăng nhập.</summary>
    [HttpGet]
    public async Task<ActionResult<List<StadiumDto>>> GetAll()
    {
        var stadiums = await _db.Stadiums
            .OrderBy(s => s.Name)
            .Select(s => new StadiumDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                City = s.City,
                Capacity = s.Capacity
            })
            .ToListAsync();

        return Ok(stadiums);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StadiumDto>> GetById(int id)
    {
        var stadium = await _db.Stadiums.FindAsync(id);
        if (stadium is null) return NotFound(new { message = "Không tìm thấy sân vận động." });

        return Ok(new StadiumDto
        {
            Id = stadium.Id,
            Name = stadium.Name,
            Address = stadium.Address,
            City = stadium.City,
            Capacity = stadium.Capacity
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<StadiumDto>> Create(StadiumUpsertDto dto)
    {
        var stadium = new Stadium
        {
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            Capacity = dto.Capacity
        };

        _db.Stadiums.Add(stadium);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = stadium.Id }, new StadiumDto
        {
            Id = stadium.Id,
            Name = stadium.Name,
            Address = stadium.Address,
            City = stadium.City,
            Capacity = stadium.Capacity
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, StadiumUpsertDto dto)
    {
        var stadium = await _db.Stadiums.FindAsync(id);
        if (stadium is null) return NotFound(new { message = "Không tìm thấy sân vận động." });

        stadium.Name = dto.Name;
        stadium.Address = dto.Address;
        stadium.City = dto.City;
        stadium.Capacity = dto.Capacity;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var stadium = await _db.Stadiums.FindAsync(id);
        if (stadium is null) return NotFound(new { message = "Không tìm thấy sân vận động." });

        _db.Stadiums.Remove(stadium);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Không thể xóa vì sân vận động này đang được đội bóng hoặc trận đấu khác sử dụng." });
        }

        return NoContent();
    }
}

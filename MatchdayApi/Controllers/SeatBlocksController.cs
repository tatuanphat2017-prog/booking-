using MatchdayApi.Data;
using MatchdayApi.DTOs.Seats;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeatBlocksController : ControllerBase
{
    private readonly AppDbContext _db;

    public SeatBlocksController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Danh sách khối ghế — công khai. Có thể lọc theo sân bằng ?stadiumId=</summary>
    [HttpGet]
    public async Task<ActionResult<List<SeatBlockDto>>> GetAll([FromQuery] int? stadiumId)
    {
        var query = _db.SeatBlocks.Include(sb => sb.Stadium).Include(sb => sb.Seats).AsQueryable();
        if (stadiumId.HasValue)
        {
            query = query.Where(sb => sb.StadiumId == stadiumId);
        }

        var blocks = await query.OrderBy(sb => sb.StadiumId).ThenBy(sb => sb.Name).ToListAsync();

        return Ok(blocks.Select(sb => new SeatBlockDto
        {
            Id = sb.Id,
            Name = sb.Name,
            BasePrice = sb.BasePrice,
            StadiumId = sb.StadiumId,
            StadiumName = sb.Stadium.Name,
            SeatCount = sb.Seats.Count
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SeatBlockDto>> GetById(int id)
    {
        var sb = await _db.SeatBlocks.Include(x => x.Stadium).Include(x => x.Seats)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (sb is null) return NotFound(new { message = "Không tìm thấy khối ghế." });

        return Ok(new SeatBlockDto
        {
            Id = sb.Id,
            Name = sb.Name,
            BasePrice = sb.BasePrice,
            StadiumId = sb.StadiumId,
            StadiumName = sb.Stadium.Name,
            SeatCount = sb.Seats.Count
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<SeatBlockDto>> Create(SeatBlockUpsertDto dto)
    {
        if (!await _db.Stadiums.AnyAsync(s => s.Id == dto.StadiumId))
        {
            return BadRequest(new { message = "StadiumId không tồn tại." });
        }

        var seatBlock = new SeatBlock
        {
            Name = dto.Name,
            BasePrice = dto.BasePrice,
            StadiumId = dto.StadiumId
        };

        _db.SeatBlocks.Add(seatBlock);
        await _db.SaveChangesAsync();
        await _db.Entry(seatBlock).Reference(x => x.Stadium).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = seatBlock.Id }, new SeatBlockDto
        {
            Id = seatBlock.Id,
            Name = seatBlock.Name,
            BasePrice = seatBlock.BasePrice,
            StadiumId = seatBlock.StadiumId,
            StadiumName = seatBlock.Stadium.Name,
            SeatCount = 0
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SeatBlockUpsertDto dto)
    {
        var seatBlock = await _db.SeatBlocks.FindAsync(id);
        if (seatBlock is null) return NotFound(new { message = "Không tìm thấy khối ghế." });

        if (!await _db.Stadiums.AnyAsync(s => s.Id == dto.StadiumId))
        {
            return BadRequest(new { message = "StadiumId không tồn tại." });
        }

        seatBlock.Name = dto.Name;
        seatBlock.BasePrice = dto.BasePrice;
        seatBlock.StadiumId = dto.StadiumId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var seatBlock = await _db.SeatBlocks.FindAsync(id);
        if (seatBlock is null) return NotFound(new { message = "Không tìm thấy khối ghế." });

        _db.SeatBlocks.Remove(seatBlock);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Không thể xóa vì khối ghế này đang có ghế hoặc giá vé liên quan." });
        }

        return NoContent();
    }
}

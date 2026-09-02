using MatchdayApi.Data;
using MatchdayApi.DTOs.Seats;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeatsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SeatsController(AppDbContext db)
    {
        _db = db;
    }

    private static SeatDto ToDto(Seat s) => new()
    {
        Id = s.Id,
        RowLabel = s.RowLabel,
        SeatNumber = s.SeatNumber,
        SeatBlockId = s.SeatBlockId,
        SeatBlockName = s.SeatBlock?.Name ?? string.Empty
    };

    /// <summary>Danh sách ghế — công khai. Bắt buộc lọc theo khối ghế bằng ?seatBlockId= (mỗi khối có hàng chục ghế).</summary>
    [HttpGet]
    public async Task<ActionResult<List<SeatDto>>> GetAll([FromQuery] int? seatBlockId)
    {
        var query = _db.Seats.Include(s => s.SeatBlock).AsQueryable();
        if (seatBlockId.HasValue)
        {
            query = query.Where(s => s.SeatBlockId == seatBlockId);
        }

        var seats = await query
            .OrderBy(s => s.SeatBlockId).ThenBy(s => s.RowLabel).ThenBy(s => s.SeatNumber)
            .ToListAsync();

        return Ok(seats.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SeatDto>> GetById(int id)
    {
        var seat = await _db.Seats.Include(s => s.SeatBlock).FirstOrDefaultAsync(s => s.Id == id);
        if (seat is null) return NotFound(new { message = "Không tìm thấy ghế." });

        return Ok(ToDto(seat));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<SeatDto>> Create(SeatUpsertDto dto)
    {
        if (!await _db.SeatBlocks.AnyAsync(sb => sb.Id == dto.SeatBlockId))
        {
            return BadRequest(new { message = "SeatBlockId không tồn tại." });
        }

        var duplicate = await _db.Seats.AnyAsync(s =>
            s.SeatBlockId == dto.SeatBlockId && s.RowLabel == dto.RowLabel && s.SeatNumber == dto.SeatNumber);
        if (duplicate)
        {
            return Conflict(new { message = "Ghế này (hàng + số) đã tồn tại trong khối ghế." });
        }

        var seat = new Seat
        {
            RowLabel = dto.RowLabel,
            SeatNumber = dto.SeatNumber,
            SeatBlockId = dto.SeatBlockId
        };

        _db.Seats.Add(seat);
        await _db.SaveChangesAsync();
        await _db.Entry(seat).Reference(s => s.SeatBlock).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = seat.Id }, ToDto(seat));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SeatUpsertDto dto)
    {
        var seat = await _db.Seats.FindAsync(id);
        if (seat is null) return NotFound(new { message = "Không tìm thấy ghế." });

        if (!await _db.SeatBlocks.AnyAsync(sb => sb.Id == dto.SeatBlockId))
        {
            return BadRequest(new { message = "SeatBlockId không tồn tại." });
        }

        seat.RowLabel = dto.RowLabel;
        seat.SeatNumber = dto.SeatNumber;
        seat.SeatBlockId = dto.SeatBlockId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var seat = await _db.Seats.FindAsync(id);
        if (seat is null) return NotFound(new { message = "Không tìm thấy ghế." });

        _db.Seats.Remove(seat);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Không thể xóa vì ghế này đang được giữ chỗ hoặc đã có trong vé đặt." });
        }

        return NoContent();
    }
}

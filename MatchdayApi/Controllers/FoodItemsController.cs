using MatchdayApi.Data;
using MatchdayApi.DTOs.FoodItems;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Controllers;

/// <summary>
/// Menu đồ ăn/thức uống bán kèm vé (task #16). CRUD cơ bản — việc "thêm đồ ăn vào 1 đơn đặt vé cụ thể"
/// nằm ở task #17 (BookingsController hoặc 1 endpoint riêng thao tác trên Booking).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FoodItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FoodItemsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Danh sách món ăn/thức uống — công khai, không cần đăng nhập.
    /// ?onlyAvailable=true để chỉ lấy món đang bán (dùng cho trang khách hàng chọn đồ ăn khi đặt vé);
    /// để trống (mặc định false) sẽ lấy TẤT CẢ kể cả món đã tắt bán — dùng cho trang quản lý của Admin.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FoodItemDto>>> GetAll([FromQuery] bool onlyAvailable = false)
    {
        var query = _db.FoodItems.AsQueryable();
        if (onlyAvailable)
        {
            query = query.Where(f => f.IsAvailable);
        }

        var items = await query
            .OrderBy(f => f.Name)
            .Select(f => new FoodItemDto
            {
                Id = f.Id,
                Name = f.Name,
                Price = f.Price,
                IsAvailable = f.IsAvailable,
                PhotoUrl = f.PhotoUrl
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FoodItemDto>> GetById(int id)
    {
        var item = await _db.FoodItems.FindAsync(id);
        if (item is null) return NotFound(new { message = "Không tìm thấy món ăn/thức uống." });

        return Ok(new FoodItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            IsAvailable = item.IsAvailable,
            PhotoUrl = item.PhotoUrl
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<FoodItemDto>> Create(FoodItemUpsertDto dto)
    {
        var item = new FoodItem
        {
            Name = dto.Name,
            Price = dto.Price,
            IsAvailable = dto.IsAvailable,
            PhotoUrl = dto.PhotoUrl
        };

        _db.FoodItems.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, new FoodItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            IsAvailable = item.IsAvailable,
            PhotoUrl = item.PhotoUrl
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, FoodItemUpsertDto dto)
    {
        var item = await _db.FoodItems.FindAsync(id);
        if (item is null) return NotFound(new { message = "Không tìm thấy món ăn/thức uống." });

        item.Name = dto.Name;
        item.Price = dto.Price;
        item.IsAvailable = dto.IsAvailable;
        item.PhotoUrl = dto.PhotoUrl;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Xóa món ăn — nếu món này đã từng được đặt kèm trong 1 đơn vé nào đó (BookingFoodItem tham chiếu tới),
    /// DB sẽ chặn xóa (Restrict) để không làm hỏng lịch sử đơn hàng cũ. Trường hợp đó nên dùng PUT để tắt
    /// "isAvailable" (ngừng bán) thay vì xóa hẳn.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.FoodItems.FindAsync(id);
        if (item is null) return NotFound(new { message = "Không tìm thấy món ăn/thức uống." });

        _db.FoodItems.Remove(item);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Không thể xóa vì món này đã từng được đặt trong 1 đơn vé. Hãy dùng PUT để tắt \"isAvailable\" (ngừng bán) thay vì xóa."
            });
        }

        return NoContent();
    }
}
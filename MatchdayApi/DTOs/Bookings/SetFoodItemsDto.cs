using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Bookings;

/// <summary>
/// Danh sách đồ ăn/thức uống muốn đặt kèm 1 Booking (task #17). Gọi PUT sẽ THAY THẾ toàn bộ giỏ đồ ăn
/// cũ của Booking đó bằng danh sách này — gửi danh sách rỗng để xóa hết đồ ăn đã chọn.
/// </summary>
public class SetFoodItemsDto
{
    public List<FoodItemQuantityDto> Items { get; set; } = new();
}

public class FoodItemQuantityDto
{
    public int FoodItemId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; }
}

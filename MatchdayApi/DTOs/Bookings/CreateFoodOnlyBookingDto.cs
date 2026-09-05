namespace MatchdayApi.DTOs.Bookings;

/// <summary>
/// Đặt thêm đồ ăn/thức uống cho 1 trận mà user ĐÃ có vé (đơn Paid, có ít nhất 1 ghế) — không kèm ghế mới.
/// Tạo ra 1 Booking riêng (không có BookingSeats), đi qua đúng luồng thanh toán VNPay như đơn vé bình
/// thường (PaymentsController không quan tâm Booking có ghế hay không).
/// </summary>
public class CreateFoodOnlyBookingDto
{
    public int MatchId { get; set; }
    public List<FoodItemQuantityDto> Items { get; set; } = new();
}
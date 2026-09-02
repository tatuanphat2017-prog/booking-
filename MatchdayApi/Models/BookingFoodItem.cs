namespace MatchdayApi.Models;

/// <summary>1 món đồ ăn/thức uống trong 1 đơn đặt vé</summary>
public class BookingFoodItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int FoodItemId { get; set; }
    public FoodItem FoodItem { get; set; } = null!;
}

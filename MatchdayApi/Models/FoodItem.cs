namespace MatchdayApi.Models;

/// <summary>Món ăn / thức uống bán kèm vé</summary>
public class FoodItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? PhotoUrl { get; set; }

    public ICollection<BookingFoodItem> BookingFoodItems { get; set; } = new List<BookingFoodItem>();
}
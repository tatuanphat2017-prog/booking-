namespace MatchdayApi.DTOs.FoodItems;

public class FoodItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public string? PhotoUrl { get; set; }
}
namespace MatchdayApi.DTOs.AdminDashboard;

public class TopFoodItemDto
{
    public int FoodItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

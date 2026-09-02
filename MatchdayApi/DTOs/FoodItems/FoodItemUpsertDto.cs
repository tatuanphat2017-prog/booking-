using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.FoodItems;

public class FoodItemUpsertDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100000000)]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}

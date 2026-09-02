using MatchdayApi.Enums;

namespace MatchdayApi.DTOs.Bookings;

public class BookingDto
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Tổng tiền vé (chỉ phần ghế) — tách riêng để frontend hiển thị breakdown.</summary>
    public decimal SeatsAmount { get; set; }

    /// <summary>Tổng tiền đồ ăn/thức uống — tách riêng để frontend hiển thị breakdown.</summary>
    public decimal FoodAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public int MatchId { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public string AwayTeamName { get; set; } = string.Empty;
    public DateTime MatchDateTime { get; set; }

    public List<BookingSeatItemDto> Seats { get; set; } = new();
    public List<BookingFoodItemDto> FoodItems { get; set; } = new();
}

public class BookingSeatItemDto
{
    public int SeatId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public string SeatBlockName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class BookingFoodItemDto
{
    public int FoodItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => Quantity * UnitPrice;
}

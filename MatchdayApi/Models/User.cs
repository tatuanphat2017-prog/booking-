namespace MatchdayApi.Models;

/// <summary>Người dùng (Admin hoặc User thường)</summary>
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public ICollection<SeatHold> SeatHolds { get; set; } = new List<SeatHold>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

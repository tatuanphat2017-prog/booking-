namespace MatchdayApi.DTOs.Auth;

/// <summary>Hồ sơ đầy đủ của user hiện tại — dùng cho trang "Bio" (account.html).</summary>
public class UserProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
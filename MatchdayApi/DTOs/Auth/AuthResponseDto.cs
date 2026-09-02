namespace MatchdayApi.DTOs.Auth;

/// <summary>Kết quả trả về sau khi đăng nhập/đăng ký thành công</summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

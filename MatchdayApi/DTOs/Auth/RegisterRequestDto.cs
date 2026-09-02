using System.ComponentModel.DataAnnotations;

namespace MatchdayApi.DTOs.Auth;

/// <summary>Dữ liệu đăng ký tài khoản mới (mặc định role = User)</summary>
public class RegisterRequestDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

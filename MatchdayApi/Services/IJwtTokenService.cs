using MatchdayApi.Models;

namespace MatchdayApi.Services;

public interface IJwtTokenService
{
    /// <summary>Sinh JWT token cho user, trả kèm thời điểm hết hạn.</summary>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}

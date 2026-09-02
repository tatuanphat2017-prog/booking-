using System.IdentityModel.Tokens.Jwt;
using MatchdayApi.Models;
using MatchdayApi.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MatchdayApi.Tests.Services;

public class JwtTokenServiceTests
{
    private static IConfiguration BuildConfig(string? key = "day-la-khoa-bi-mat-du-dai-de-test-hmacsha256-1234567890")
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "MatchdayApi",
            ["Jwt:Audience"] = "MatchdayApiClient",
            ["Jwt:ExpiryMinutes"] = "120"
        };
        if (key is not null) dict["Jwt:Key"] = key;

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static User BuildUser(string roleName) => new()
    {
        Id = 7,
        FullName = "Nguyễn Văn Test",
        Email = "test@matchday.local",
        PasswordHash = "x",
        Role = new Role { Id = roleName == "Admin" ? 1 : 2, Name = roleName }
    };

    [Fact]
    public void GenerateToken_ShouldReturnValidJwt_WithCorrectClaims()
    {
        var service = new JwtTokenService(BuildConfig());
        var user = BuildUser("User");

        var (token, expiresAt) = service.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("MatchdayApi", jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == "email" && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type.EndsWith("/role") && c.Value == "User");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeAdminRole_ForAdminUser()
    {
        var service = new JwtTokenService(BuildConfig());
        var user = BuildUser("Admin");

        var (token, _) = service.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type.EndsWith("/role") && c.Value == "Admin");
    }

    [Fact]
    public void GenerateToken_ShouldThrow_WhenJwtKeyMissing()
    {
        var service = new JwtTokenService(BuildConfig(key: null));
        var user = BuildUser("User");

        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(user));
    }
}

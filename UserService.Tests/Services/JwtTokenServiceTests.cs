using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using UserService.Configuration;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service = new(Options.Create(new JwtOptions
    {
        Secret = "test-secret-key-at-least-32-characters-long",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpiresInSeconds = 3600
    }));

    [Fact]
    public void GenerateToken_ProducesNonEmptyToken()
    {
        var token = _service.GenerateToken(new User { UserId = Guid.NewGuid(), Email = "test@example.com", Role = Role.Patron });
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_EncodesUserIdAndRoleClaims()
    {
        var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", Role = Role.Librarian };
        var token = _service.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.UserId.ToString(), jwt.Claims.First(c => c.Type == "userId").Value);
        Assert.Equal("LIBRARIAN", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }
}
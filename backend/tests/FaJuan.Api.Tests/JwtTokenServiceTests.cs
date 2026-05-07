using System.IdentityModel.Tokens.Jwt;
using FaJuan.Api.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;

namespace FaJuan.Api.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateMiniAppToken_Should_UseUtcExpiration_And_ReturnMatchingExpiresAt()
    {
        var configuration = BuildConfiguration();
        var sut = new JwtTokenService(configuration);

        var before = DateTimeOffset.UtcNow;
        var result = sut.CreateMiniAppToken(123);
        var after = DateTimeOffset.UtcNow;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        var expectedMin = before.AddDays(7).AddMinutes(-1);
        var expectedMax = after.AddDays(7).AddMinutes(1);

        Assert.Equal("FaJuan.MiniApp", token.Audiences.Single());
        Assert.Equal(DateTimeKind.Utc, token.ValidTo.Kind);
        Assert.InRange(token.ValidTo, expectedMin.UtcDateTime, expectedMax.UtcDateTime);
        Assert.True(Math.Abs((token.ValidTo - result.ExpiresAt.UtcDateTime).TotalSeconds) < 1);
    }

    [Fact]
    public void CreateAdminToken_Should_UseUtcExpiration_And_ReturnMatchingExpiresAt()
    {
        var configuration = BuildConfiguration();
        var sut = new JwtTokenService(configuration);

        var before = DateTimeOffset.UtcNow;
        var result = sut.CreateAdminToken("admin");
        var after = DateTimeOffset.UtcNow;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        var expectedMin = before.AddMinutes(120).AddMinutes(-1);
        var expectedMax = after.AddMinutes(120).AddMinutes(1);

        Assert.Equal("FaJuan.Admin", token.Audiences.Single());
        Assert.Equal(DateTimeKind.Utc, token.ValidTo.Kind);
        Assert.InRange(token.ValidTo, expectedMin.UtcDateTime, expectedMax.UtcDateTime);
        Assert.True(Math.Abs((token.ValidTo - result.ExpiresAt.UtcDateTime).TotalSeconds) < 1);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "FaJuan.Api",
                ["Jwt:Audience"] = "FaJuan.Admin",
                ["Jwt:SecurityKey"] = "12345678901234567890123456789012",
                ["Jwt:ExpireMinutes"] = "120",
            })
            .Build();
    }
}

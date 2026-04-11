using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FaJuan.Api.Infrastructure.Auth;

public class JwtTokenService(IConfiguration configuration)
{
    public (string AccessToken, DateTimeOffset ExpiresAt) CreateAdminToken(string username)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "FaJuan.Api";
        var audience = configuration["Jwt:Audience"] ?? "FaJuan.Admin";
        var securityKey = configuration["Jwt:SecurityKey"] ?? throw new InvalidOperationException("Jwt:SecurityKey 未配置");
        var expireMinutes = int.TryParse(configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 720;
        var expiresAt = DateTimeOffset.Now.AddMinutes(expireMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Admin"),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt.LocalDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string AccessToken, DateTimeOffset ExpiresAt) CreateMiniAppToken(long userId)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "FaJuan.Api";
        var securityKey = configuration["Jwt:SecurityKey"] ?? throw new InvalidOperationException("Jwt:SecurityKey 未配置");
        var expiresAt = DateTimeOffset.Now.AddDays(7);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("userId", userId.ToString()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: "FaJuan.MiniApp",
            claims: claims,
            expires: expiresAt.LocalDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FaJuan.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace FaJuan.Api.Infrastructure.Auth;

public class MiniAppAuthorizeFilter(ILogger<MiniAppAuthorizeFilter> logger, IConfiguration configuration) : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata.OfType<MiniAppAuthorizeAttribute>().FirstOrDefault();
        if (attribute is null)
        {
            return Task.CompletedTask;
        }

        var authHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            if (attribute.Optional)
            {
                return Task.CompletedTask;
            }

            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("请先登录", 401));
            return Task.CompletedTask;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var issuer = configuration["Jwt:Issuer"] ?? "FaJuan.Api";
            var securityKey = configuration["Jwt:SecurityKey"] ?? throw new InvalidOperationException("Jwt:SecurityKey 未配置");

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = "FaJuan.MiniApp",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            context.HttpContext.User = principal;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MiniApp token validation failed");
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("登录状态已失效", 401));
        }

        return Task.CompletedTask;
    }
}

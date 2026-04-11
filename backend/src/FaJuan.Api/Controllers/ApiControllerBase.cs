using System.Security.Claims;
using FaJuan.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiResponse<T> Success<T>(T? data, string message = "success") => ApiResponse<T>.Ok(data, message);

    protected ApiResponse<T> Failure<T>(string message, int code = 400) => ApiResponse<T>.Fail(message, code);

    protected long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && long.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

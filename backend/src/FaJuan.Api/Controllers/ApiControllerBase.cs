using FaJuan.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiResponse<T> Success<T>(T? data, string message = "success") => ApiResponse<T>.Ok(data, message);

    protected ApiResponse<T> Failure<T>(string message, int code = 400) => ApiResponse<T>.Fail(message, code);
}

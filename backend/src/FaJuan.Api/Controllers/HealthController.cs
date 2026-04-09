using FaJuan.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Controllers;

public class HealthController : ApiControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<HealthStatusDto>> Get()
    {
        var dto = new HealthStatusDto
        {
            Environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName,
            ServerTime = DateTimeOffset.Now
        };

        return Ok(Success(dto));
    }
}

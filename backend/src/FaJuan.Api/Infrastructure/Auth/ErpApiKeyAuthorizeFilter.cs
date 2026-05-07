using FaJuan.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Infrastructure.Auth;

public class ErpApiKeyAuthorizeFilter(IOptions<ErpApiOptions> options) : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata.OfType<ErpApiKeyAuthorizeAttribute>().FirstOrDefault();
        if (attribute is null)
        {
            return Task.CompletedTask;
        }

        var config = options.Value;
        var headerName = string.IsNullOrWhiteSpace(config.ApiKeyHeaderName) ? "X-Api-Key" : config.ApiKeyHeaderName.Trim();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("ERP API Key 未配置", 503)) { StatusCode = 503 };
            return Task.CompletedTask;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(headerName, out var requestKey) ||
            string.IsNullOrWhiteSpace(requestKey.ToString()))
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("缺少 ERP API Key", 401)) { StatusCode = 401 };
            return Task.CompletedTask;
        }

        if (!string.Equals(requestKey.ToString().Trim(), config.ApiKey.Trim(), StringComparison.Ordinal))
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("ERP API Key 无效", 401)) { StatusCode = 401 };
        }

        return Task.CompletedTask;
    }
}

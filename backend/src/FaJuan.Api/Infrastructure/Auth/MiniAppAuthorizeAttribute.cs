using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Infrastructure.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class MiniAppAuthorizeAttribute(bool optional = false) : TypeFilterAttribute(typeof(MiniAppAuthorizeFilter))
{
    public bool Optional { get; set; } = optional;
}

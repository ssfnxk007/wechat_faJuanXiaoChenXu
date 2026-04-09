using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Infrastructure.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AdminMenuAuthorizeAttribute(string menuPath) : TypeFilterAttribute(typeof(AdminMenuAuthorizeFilter))
{
    public string MenuPath { get; } = menuPath;
}

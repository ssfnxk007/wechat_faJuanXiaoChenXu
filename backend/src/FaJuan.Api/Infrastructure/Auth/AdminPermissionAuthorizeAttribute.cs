using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Infrastructure.Auth;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AdminPermissionAuthorizeAttribute(string permissionCode) : TypeFilterAttribute(typeof(AdminPermissionAuthorizeFilter))
{
    public string PermissionCode { get; } = permissionCode;
}

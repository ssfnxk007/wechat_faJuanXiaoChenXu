using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Infrastructure.Auth;

public class AdminPermissionAuthorizeFilter(AppDbContext dbContext, IConfiguration configuration) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata.OfType<AdminPermissionAuthorizeAttribute>().FirstOrDefault();
        if (attribute is null)
        {
            return;
        }

        var username = context.HttpContext.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("登录状态已失效", 401));
            return;
        }

        var hasDbAdmins = await dbContext.AdminUsers.AsNoTracking().AnyAsync();
        var fallbackAdmin = configuration["AdminAuth:Username"] ?? "admin";
        if (!hasDbAdmins && string.Equals(username, fallbackAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var adminUser = await dbContext.AdminUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Username == username && x.IsEnabled);
        if (adminUser is null)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("无权限访问该功能", 403)) { StatusCode = 403 };
            return;
        }

        var roleIds = await dbContext.AdminUserRoles.AsNoTracking()
            .Where(x => x.AdminUserId == adminUser.Id)
            .Join(dbContext.AdminRoles.AsNoTracking().Where(x => x.IsEnabled), x => x.AdminRoleId, x => x.Id, (userRole, role) => role.Id)
            .Distinct()
            .ToListAsync();

        if (roleIds.Count == 0)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("当前账号未分配角色", 403)) { StatusCode = 403 };
            return;
        }

        var hasPermission = await dbContext.AdminRolePermissions.AsNoTracking()
            .Where(x => roleIds.Contains(x.AdminRoleId))
            .Join(dbContext.AdminPermissions.AsNoTracking().Where(x => x.IsEnabled), x => x.AdminPermissionId, x => x.Id, (rolePermission, permission) => permission.Code)
            .AnyAsync(x => x == attribute.PermissionCode);

        if (!hasPermission)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("当前账号无此按钮权限", 403)) { StatusCode = 403 };
        }
    }
}

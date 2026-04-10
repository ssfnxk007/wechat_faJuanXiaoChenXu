using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Infrastructure.Auth;

public class AdminMenuAuthorizeFilter(AppDbContext dbContext, IConfiguration configuration) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata.OfType<AdminMenuAuthorizeAttribute>().FirstOrDefault();
        if (attribute is null)
        {
            return;
        }

        var username = context.HttpContext.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("???????", 401));
            return;
        }

        var hasDbAdmins = await dbContext.AdminUsers.AsNoTracking().AnyAsync();
        var fallbackAdmin = configuration["AdminAuth:Username"] ?? "admin";
        if (!hasDbAdmins && string.Equals(username, fallbackAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var adminUser = await dbContext.AdminUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username && x.IsEnabled);
        if (adminUser is null)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("????????", 403)) { StatusCode = 403 };
            return;
        }

        var hasRole = await dbContext.AdminUserRoles.AsNoTracking()
            .Where(x => x.AdminUserId == adminUser.Id)
            .Join(
                dbContext.AdminRoles.AsNoTracking().Where(x => x.IsEnabled),
                userRole => userRole.AdminRoleId,
                role => role.Id,
                (_, _) => 1)
            .AnyAsync();

        if (!hasRole)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("?????????", 403)) { StatusCode = 403 };
            return;
        }

        var hasMenuPermission = await dbContext.AdminUserRoles.AsNoTracking()
            .Where(x => x.AdminUserId == adminUser.Id)
            .Join(
                dbContext.AdminRoles.AsNoTracking().Where(x => x.IsEnabled),
                userRole => userRole.AdminRoleId,
                role => role.Id,
                (userRole, _) => userRole.AdminRoleId)
            .Join(
                dbContext.AdminRoleMenus.AsNoTracking(),
                roleId => roleId,
                roleMenu => roleMenu.AdminRoleId,
                (_, roleMenu) => roleMenu.AdminMenuId)
            .Join(
                dbContext.AdminMenus.AsNoTracking().Where(x => x.IsEnabled),
                menuId => menuId,
                menu => menu.Id,
                (_, menu) => menu.Path)
            .AnyAsync(path => path == attribute.MenuPath);

        if (!hasMenuPermission)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("??????????", 403)) { StatusCode = 403 };
        }
    }
}

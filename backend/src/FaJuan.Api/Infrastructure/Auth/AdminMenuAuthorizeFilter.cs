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

        var roleIds = await dbContext.AdminUserRoles.AsNoTracking()
            .Where(x => x.AdminUserId == adminUser.Id)
            .Join(
                dbContext.AdminRoles.AsNoTracking().Where(x => x.IsEnabled),
                userRole => userRole.AdminRoleId,
                role => role.Id,
                (userRole, role) => role.Id)
            .Distinct()
            .ToListAsync();

        if (roleIds.Count == 0)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("?????????", 403)) { StatusCode = 403 };
            return;
        }

        var hasMenuPermission = await dbContext.AdminRoleMenus.AsNoTracking()
            .Where(x => roleIds.Contains(x.AdminRoleId))
            .Join(
                dbContext.AdminMenus.AsNoTracking().Where(x => x.IsEnabled),
                roleMenu => roleMenu.AdminMenuId,
                menu => menu.Id,
                (roleMenu, menu) => menu.Path)
            .AnyAsync(path => path == attribute.MenuPath);

        if (!hasMenuPermission)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("??????????", 403)) { StatusCode = 403 };
        }
    }
}

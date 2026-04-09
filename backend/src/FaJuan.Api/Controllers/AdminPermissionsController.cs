using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/admin-roles")]
public class AdminPermissionsController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminPermissionListItemDto>>>> GetList()
    {
        var items = await dbContext.AdminPermissions.AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.MenuPath)
            .ThenBy(x => x.Sort)
            .ThenBy(x => x.Id)
            .Select(x => new AdminPermissionListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                MenuPath = x.MenuPath,
                Sort = x.Sort,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        return Ok(Success<IReadOnlyCollection<AdminPermissionListItemDto>>(items));
    }
}

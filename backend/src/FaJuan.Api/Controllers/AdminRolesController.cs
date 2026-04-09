using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/admin-roles")]
public class AdminRolesController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminRoleListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
    {
        var query = dbContext.AdminRoles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminRoleListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        var roleIds = items.Select(x => x.Id).ToArray();
        var userCounts = roleIds.Length == 0
            ? new Dictionary<long, int>()
            : await dbContext.AdminUserRoles.AsNoTracking()
                .Where(x => roleIds.Contains(x.AdminRoleId))
                .GroupBy(x => x.AdminRoleId)
                .Select(x => new { RoleId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count);

        var roleMenus = roleIds.Length == 0
            ? new List<dynamic>()
            : await dbContext.AdminRoleMenus.AsNoTracking()
                .Where(x => roleIds.Contains(x.AdminRoleId))
                .Select(x => new { x.AdminRoleId, x.AdminMenuId })
                .ToListAsync<dynamic>();

        var rolePermissions = roleIds.Length == 0
            ? new List<dynamic>()
            : await dbContext.AdminRolePermissions.AsNoTracking()
                .Where(x => roleIds.Contains(x.AdminRoleId))
                .Select(x => new { x.AdminRoleId, x.AdminPermissionId })
                .ToListAsync<dynamic>();

        foreach (var item in items)
        {
            item.UserCount = userCounts.TryGetValue(item.Id, out var userCount) ? userCount : 0;
            var menuIds = roleMenus.Where(x => x.AdminRoleId == item.Id).Select(x => (long)x.AdminMenuId).ToArray();
            var permissionIds = rolePermissions.Where(x => x.AdminRoleId == item.Id).Select(x => (long)x.AdminPermissionId).ToArray();
            item.MenuIds = menuIds;
            item.MenuCount = menuIds.Length;
            item.PermissionIds = permissionIds;
            item.PermissionCount = permissionIds.Length;
        }

        return Ok(Success(new PagedResult<AdminRoleListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("admin.role.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveAdminRoleRequest request)
    {
        var validationError = ValidateSaveRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var code = request.Code.Trim();
        var exists = await dbContext.AdminRoles.AnyAsync(x => x.Code == code);
        if (exists)
        {
            return BadRequest(Failure<long>("???????"));
        }

        var menuIds = request.MenuIds.Distinct().ToArray();
        if (menuIds.Length > 0)
        {
            var menuCount = await dbContext.AdminMenus.CountAsync(x => menuIds.Contains(x.Id));
            if (menuCount != menuIds.Length)
            {
                return BadRequest(Failure<long>("???????????"));
            }
        }

        var permissionIds = request.PermissionIds.Distinct().ToArray();
        if (permissionIds.Length > 0)
        {
            var permissionCount = await dbContext.AdminPermissions.CountAsync(x => permissionIds.Contains(x.Id));
            if (permissionCount != permissionIds.Length)
            {
                return BadRequest(Failure<long>("????????????"));
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var entity = new AdminRole
        {
            Name = request.Name.Trim(),
            Code = code,
            IsEnabled = request.IsEnabled,
        };

        dbContext.AdminRoles.Add(entity);
        await dbContext.SaveChangesAsync();

        if (menuIds.Length > 0)
        {
            dbContext.AdminRoleMenus.AddRange(menuIds.Select(menuId => new AdminRoleMenu
            {
                AdminRoleId = entity.Id,
                AdminMenuId = menuId,
            }));
        }

        if (permissionIds.Length > 0)
        {
            dbContext.AdminRolePermissions.AddRange(permissionIds.Select(permissionId => new AdminRolePermission
            {
                AdminRoleId = entity.Id,
                AdminPermissionId = permissionId,
            }));
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(Success(entity.Id, "????"));
    }

    [AdminPermissionAuthorize("admin.role.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveAdminRoleRequest request)
    {
        var validationError = ValidateSaveRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.AdminRoles.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("?????"));
        }

        var code = request.Code.Trim();
        var duplicated = await dbContext.AdminRoles.AnyAsync(x => x.Id != id && x.Code == code);
        if (duplicated)
        {
            return BadRequest(Failure<long>("???????"));
        }

        var menuIds = request.MenuIds.Distinct().ToArray();
        if (menuIds.Length > 0)
        {
            var menuCount = await dbContext.AdminMenus.CountAsync(x => menuIds.Contains(x.Id));
            if (menuCount != menuIds.Length)
            {
                return BadRequest(Failure<long>("???????????"));
            }
        }

        var permissionIds = request.PermissionIds.Distinct().ToArray();
        if (permissionIds.Length > 0)
        {
            var permissionCount = await dbContext.AdminPermissions.CountAsync(x => permissionIds.Contains(x.Id));
            if (permissionCount != permissionIds.Length)
            {
                return BadRequest(Failure<long>("????????????"));
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        entity.Name = request.Name.Trim();
        entity.Code = code;
        entity.IsEnabled = request.IsEnabled;

        var roleMenus = await dbContext.AdminRoleMenus.Where(x => x.AdminRoleId == id).ToListAsync();
        var rolePermissionsToRemove = await dbContext.AdminRolePermissions.Where(x => x.AdminRoleId == id).ToListAsync();
        dbContext.AdminRoleMenus.RemoveRange(roleMenus);
        dbContext.AdminRolePermissions.RemoveRange(rolePermissionsToRemove);
        if (menuIds.Length > 0)
        {
            dbContext.AdminRoleMenus.AddRange(menuIds.Select(menuId => new AdminRoleMenu
            {
                AdminRoleId = id,
                AdminMenuId = menuId,
            }));
        }
        if (permissionIds.Length > 0)
        {
            dbContext.AdminRolePermissions.AddRange(permissionIds.Select(permissionId => new AdminRolePermission
            {
                AdminRoleId = id,
                AdminPermissionId = permissionId,
            }));
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(Success(id, "????"));
    }

    [AdminPermissionAuthorize("admin.role.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.AdminRoles.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("?????"));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var userRoles = await dbContext.AdminUserRoles.Where(x => x.AdminRoleId == id).ToListAsync();
        var roleMenus = await dbContext.AdminRoleMenus.Where(x => x.AdminRoleId == id).ToListAsync();
        var rolePermissions = await dbContext.AdminRolePermissions.Where(x => x.AdminRoleId == id).ToListAsync();
        dbContext.AdminUserRoles.RemoveRange(userRoles);
        dbContext.AdminRoleMenus.RemoveRange(roleMenus);
        dbContext.AdminRolePermissions.RemoveRange(rolePermissions);
        dbContext.AdminRoles.Remove(entity);
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(Success(true, "????"));
    }

    private static string? ValidateSaveRequest(SaveAdminRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return "???????????";
        }

        return null;
    }
}

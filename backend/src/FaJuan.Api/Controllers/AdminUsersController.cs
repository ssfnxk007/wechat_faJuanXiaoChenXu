using FaJuan.Api.Application.Common;
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
[AdminMenuAuthorize("/admin-users")]
public class AdminUsersController(AppDbContext dbContext, PasswordHashService passwordHashService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.AdminUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Username.Contains(keyword) || x.DisplayName.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new AdminUserListItemDto
            {
                Id = x.Id,
                Username = x.Username,
                DisplayName = x.DisplayName,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        var userIds = items.Select(x => x.Id).ToHashSet();
        var roleRows = items.Count == 0
            ? new List<dynamic>()
            : (await dbContext.AdminUserRoles.AsNoTracking()
                .Join(
                    dbContext.AdminRoles.AsNoTracking(),
                    userRole => userRole.AdminRoleId,
                    role => role.Id,
                    (userRole, role) => new
                    {
                        userRole.AdminUserId,
                        RoleId = role.Id,
                        RoleName = role.Name,
                    })
                .ToListAsync())
                .Where(x => userIds.Contains(x.AdminUserId))
                .Cast<dynamic>()
                .ToList();

        foreach (var item in items)
        {
            var roles = roleRows.Where(x => x.AdminUserId == item.Id).ToList();
            item.RoleIds = roles.Select(x => (long)x.RoleId).ToArray();
            item.RoleNames = string.Join('?', roles.Select(x => (string)x.RoleName));
        }

        return Ok(Success(new PagedResult<AdminUserListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("admin.user.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveAdminUserRequest request)
    {
        var validationError = ValidateSaveRequest(request, true);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var username = request.Username.Trim();
        var exists = await dbContext.AdminUsers.AnyAsync(x => x.Username == username);
        if (exists)
        {
            return BadRequest(Failure<long>("????????"));
        }

        var roleIds = request.RoleIds.Distinct().ToArray();
        if (roleIds.Length > 0)
        {
            var existingRoleIds = await dbContext.AdminRoles.AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();
            var roleCount = existingRoleIds.Count(x => roleIds.Contains(x));
            if (roleCount != roleIds.Length)
            {
                return BadRequest(Failure<long>("???????????"));
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var entity = new AdminUser
        {
            Username = username,
            PasswordHash = passwordHashService.Hash(request.Password!.Trim()),
            DisplayName = request.DisplayName.Trim(),
            IsEnabled = request.IsEnabled,
        };

        dbContext.AdminUsers.Add(entity);
        await dbContext.SaveChangesAsync();

        if (roleIds.Length > 0)
        {
            dbContext.AdminUserRoles.AddRange(roleIds.Select(roleId => new AdminUserRole
            {
                AdminUserId = entity.Id,
                AdminRoleId = roleId,
            }));
            await dbContext.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return Ok(Success(entity.Id, "????"));
    }

    [AdminPermissionAuthorize("admin.user.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveAdminUserRequest request)
    {
        var validationError = ValidateSaveRequest(request, false);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("??????"));
        }

        var username = request.Username.Trim();
        var duplicated = await dbContext.AdminUsers.AnyAsync(x => x.Id != id && x.Username == username);
        if (duplicated)
        {
            return BadRequest(Failure<long>("????????"));
        }

        var roleIds = request.RoleIds.Distinct().ToArray();
        if (roleIds.Length > 0)
        {
            var existingRoleIds = await dbContext.AdminRoles.AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();
            var roleCount = existingRoleIds.Count(x => roleIds.Contains(x));
            if (roleCount != roleIds.Length)
            {
                return BadRequest(Failure<long>("???????????"));
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        entity.Username = username;
        entity.DisplayName = request.DisplayName.Trim();
        entity.IsEnabled = request.IsEnabled;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            entity.PasswordHash = passwordHashService.Hash(request.Password.Trim());
        }

        var userRoles = await dbContext.AdminUserRoles.Where(x => x.AdminUserId == id).ToListAsync();
        dbContext.AdminUserRoles.RemoveRange(userRoles);
        if (roleIds.Length > 0)
        {
            dbContext.AdminUserRoles.AddRange(roleIds.Select(roleId => new AdminUserRole
            {
                AdminUserId = id,
                AdminRoleId = roleId,
            }));
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(Success(id, "????"));
    }

    [AdminPermissionAuthorize("admin.user.reset-password")]
    [HttpPost("{id:long}/reset-password")]
    public async Task<ActionResult<ApiResponse<long>>> ResetPassword(long id, [FromBody] ResetAdminUserPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Trim().Length < 6)
        {
            return BadRequest(Failure<long>("????? 6 ?"));
        }

        var entity = await dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("??????"));
        }

        entity.PasswordHash = passwordHashService.Hash(request.Password.Trim());
        await dbContext.SaveChangesAsync();
        return Ok(Success(id, "??????"));
    }

    [AdminPermissionAuthorize("admin.user.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("??????"));
        }

        var currentUsername = User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(currentUsername) && string.Equals(entity.Username, currentUsername, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(Failure<bool>("???????????"));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var roles = await dbContext.AdminUserRoles.Where(x => x.AdminUserId == id).ToListAsync();
        dbContext.AdminUserRoles.RemoveRange(roles);
        dbContext.AdminUsers.Remove(entity);
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(Success(true, "????"));
    }

    private static string? ValidateSaveRequest(SaveAdminUserRequest request, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return "?????????";
        }

        if (isCreate && (string.IsNullOrWhiteSpace(request.Password) || request.Password.Trim().Length < 6))
        {
            return "?????? 6 ?";
        }

        return null;
    }
}

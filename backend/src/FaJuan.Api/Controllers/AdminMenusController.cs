using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/admin-menus")]
public class AdminMenusController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminMenuListItemDto>>>> GetList()
    {
        var items = await dbContext.AdminMenus.AsNoTracking()
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.Id)
            .Select(x => new AdminMenuListItemDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Name = x.Name,
                Path = x.Path,
                Component = x.Component,
                Sort = x.Sort,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        return Ok(Success<IReadOnlyCollection<AdminMenuListItemDto>>(BuildTree(items, null)));
    }

    [AdminPermissionAuthorize("admin.menu.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveAdminMenuRequest request)
    {
        var validationError = await ValidateSaveRequest(request, null);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = new AdminMenu
        {
            ParentId = request.ParentId,
            Name = request.Name.Trim(),
            Path = request.Path.Trim(),
            Component = request.Component.Trim(),
            Sort = request.Sort,
            IsEnabled = request.IsEnabled,
        };

        dbContext.AdminMenus.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "????"));
    }

    [AdminPermissionAuthorize("admin.menu.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveAdminMenuRequest request)
    {
        var entity = await dbContext.AdminMenus.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("?????"));
        }

        var validationError = await ValidateSaveRequest(request, id);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        entity.ParentId = request.ParentId;
        entity.Name = request.Name.Trim();
        entity.Path = request.Path.Trim();
        entity.Component = request.Component.Trim();
        entity.Sort = request.Sort;
        entity.IsEnabled = request.IsEnabled;

        await dbContext.SaveChangesAsync();
        return Ok(Success(id, "????"));
    }

    [AdminPermissionAuthorize("admin.menu.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.AdminMenus.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("?????"));
        }

        var hasChildren = await dbContext.AdminMenus.AnyAsync(x => x.ParentId == id);
        if (hasChildren)
        {
            return BadRequest(Failure<bool>("????????"));
        }

        var usedByRole = await dbContext.AdminRoleMenus.AnyAsync(x => x.AdminMenuId == id);
        if (usedByRole)
        {
            return BadRequest(Failure<bool>("???????????????"));
        }

        dbContext.AdminMenus.Remove(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(true, "????"));
    }

    private async Task<string?> ValidateSaveRequest(SaveAdminMenuRequest request, long? currentId)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Path) || string.IsNullOrWhiteSpace(request.Component))
        {
            return "??????????????";
        }

        if (request.ParentId.HasValue)
        {
            if (currentId.HasValue && request.ParentId.Value == currentId.Value)
            {
                return "??????????";
            }

            var parentExists = await dbContext.AdminMenus.AnyAsync(x => x.Id == request.ParentId.Value);
            if (!parentExists)
            {
                return "???????";
            }
        }

        var duplicatedPath = await dbContext.AdminMenus.AnyAsync(x => x.Path == request.Path.Trim() && (!currentId.HasValue || x.Id != currentId.Value));
        if (duplicatedPath)
        {
            return "???????";
        }

        return null;
    }

    private static IReadOnlyCollection<AdminMenuListItemDto> BuildTree(IReadOnlyCollection<AdminMenuListItemDto> source, long? parentId)
    {
        return source
            .Where(x => x.ParentId == parentId)
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.Id)
            .Select(x => new AdminMenuListItemDto
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Name = x.Name,
                Path = x.Path,
                Component = x.Component,
                Sort = x.Sort,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
                Children = BuildTree(source, x.Id),
            })
            .ToArray();
    }
}

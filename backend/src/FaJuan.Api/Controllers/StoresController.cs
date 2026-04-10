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
[AdminMenuAuthorize("/stores")]
public class StoresController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<StoreListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.Stores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new StoreListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                ContactName = x.ContactName,
                ContactPhone = x.ContactPhone,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        return Ok(Success(new PagedResult<StoreListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("store.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveStoreRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var normalizedCode = request.Code.Trim();
        var exists = await dbContext.Stores.AnyAsync(x => x.Code == normalizedCode);
        if (exists)
        {
            return BadRequest(Failure<long>("门店编码已存在"));
        }

        var entity = new Store
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            ContactName = request.ContactName?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            IsEnabled = request.IsEnabled,
        };

        dbContext.Stores.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "创建成功"));
    }

    [AdminPermissionAuthorize("store.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveStoreRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.Stores.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("门店不存在"));
        }

        var normalizedCode = request.Code.Trim();
        var duplicatedCode = await dbContext.Stores.AnyAsync(x => x.Id != id && x.Code == normalizedCode);
        if (duplicatedCode)
        {
            return BadRequest(Failure<long>("门店编码已存在"));
        }

        entity.Code = normalizedCode;
        entity.Name = request.Name.Trim();
        entity.ContactName = request.ContactName?.Trim();
        entity.ContactPhone = request.ContactPhone?.Trim();
        entity.IsEnabled = request.IsEnabled;

        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "更新成功"));
    }

    [AdminPermissionAuthorize("store.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.Stores.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("门店不存在"));
        }

        dbContext.Stores.Remove(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(true, "删除成功"));
    }

    private static string? ValidateRequest(SaveStoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return "门店编码和名称不能为空";
        }

        return null;
    }
}

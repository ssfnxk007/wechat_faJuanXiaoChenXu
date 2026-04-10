using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/coupon-packs")]
public class CouponPacksController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CouponPackListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.CouponPacks.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new CouponPackListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageAssetId = x.ImageAssetId,
                SalePrice = x.SalePrice,
                Status = (int)x.Status,
                PerUserLimit = x.PerUserLimit,
                SaleStartTime = x.SaleStartTime,
                SaleEndTime = x.SaleEndTime,
                Remark = x.Remark,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        var assetIds = items.Where(x => x.ImageAssetId.HasValue).Select(x => x.ImageAssetId!.Value).Distinct().ToArray();
        var assetMap = assetIds.Length == 0
            ? new Dictionary<long, string>()
            : await dbContext.MediaAssets.AsNoTracking()
                .Where(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileUrl);

        items = items.Select(x => new CouponPackListItemDto
        {
            Id = x.Id,
            Name = x.Name,
            ImageAssetId = x.ImageAssetId,
            ImageUrl = x.ImageAssetId.HasValue && assetMap.TryGetValue(x.ImageAssetId.Value, out var imageUrl) ? imageUrl : null,
            SalePrice = x.SalePrice,
            Status = x.Status,
            PerUserLimit = x.PerUserLimit,
            SaleStartTime = x.SaleStartTime,
            SaleEndTime = x.SaleEndTime,
            Remark = x.Remark,
            CreatedAt = x.CreatedAt,
        }).ToList();

        return Ok(Success(new PagedResult<CouponPackListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("coupon-pack.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveCouponPackRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = new CouponPack
        {
            Name = request.Name.Trim(),
            ImageAssetId = request.ImageAssetId,
            SalePrice = request.SalePrice,
            Status = (CouponPackStatus)request.Status,
            PerUserLimit = request.PerUserLimit,
            SaleStartTime = request.SaleStartTime,
            SaleEndTime = request.SaleEndTime,
            Remark = request.Remark?.Trim(),
        };

        dbContext.CouponPacks.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "创建成功"));
    }

    [AdminPermissionAuthorize("coupon-pack.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveCouponPackRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.CouponPacks.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("券包不存在"));
        }

        entity.Name = request.Name.Trim();
        entity.ImageAssetId = request.ImageAssetId;
        entity.SalePrice = request.SalePrice;
        entity.Status = (CouponPackStatus)request.Status;
        entity.PerUserLimit = request.PerUserLimit;
        entity.SaleStartTime = request.SaleStartTime;
        entity.SaleEndTime = request.SaleEndTime;
        entity.Remark = request.Remark?.Trim();

        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "更新成功"));
    }

    [AdminPermissionAuthorize("coupon-pack.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.CouponPacks.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("券包不存在"));
        }

        dbContext.CouponPacks.Remove(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(true, "删除成功"));
    }

    private static string? ValidateRequest(SaveCouponPackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "券包名称不能为空";
        }

        if (request.SalePrice <= 0)
        {
            return "券包售价必须大于 0";
        }

        return null;
    }
}

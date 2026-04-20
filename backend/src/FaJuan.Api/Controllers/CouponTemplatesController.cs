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
[AdminMenuAuthorize("/coupon-templates")]
public class CouponTemplatesController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CouponTemplateListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.CouponTemplates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new CouponTemplateListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageAssetId = x.ImageAssetId,
                TemplateType = x.TemplateType,
                ValidPeriodType = x.ValidPeriodType,
                DiscountAmount = x.DiscountAmount,
                ThresholdAmount = x.ThresholdAmount,
                ValidDays = x.ValidDays,
                ValidFrom = x.ValidFrom,
                ValidTo = x.ValidTo,
                IsNewUserOnly = x.IsNewUserOnly,
                IsAllStores = x.IsAllStores,
                PerUserLimit = x.PerUserLimit,
                IsEnabled = x.IsEnabled,
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

        var templateIds = items.Select(x => x.Id).ToHashSet();
        var scopes = items.Count == 0
            ? new Dictionary<long, IReadOnlyCollection<long>>()
            : (await dbContext.CouponTemplateProductScopes.AsNoTracking()
                .GroupBy(x => x.CouponTemplateId)
                .Select(x => new { x.Key, ProductIds = x.Select(y => y.ProductId).ToList() })
                .ToListAsync())
                .Where(x => templateIds.Contains(x.Key))
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<long>)x.ProductIds);

        var storeScopes = items.Count == 0
            ? new Dictionary<long, IReadOnlyCollection<long>>()
            : (await dbContext.CouponTemplateStoreScopes.AsNoTracking()
                .GroupBy(x => x.CouponTemplateId)
                .Select(x => new { x.Key, StoreIds = x.Select(y => y.StoreId).ToList() })
                .ToListAsync())
                .Where(x => templateIds.Contains(x.Key))
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<long>)x.StoreIds);

        items = items.Select(x => new CouponTemplateListItemDto
        {
            Id = x.Id,
            Name = x.Name,
            ImageAssetId = x.ImageAssetId,
            ImageUrl = x.ImageAssetId.HasValue && assetMap.TryGetValue(x.ImageAssetId.Value, out var imageUrl) ? imageUrl : null,
            TemplateType = x.TemplateType,
            ValidPeriodType = x.ValidPeriodType,
            DiscountAmount = x.DiscountAmount,
            ThresholdAmount = x.ThresholdAmount,
            ValidDays = x.ValidDays,
            ValidFrom = x.ValidFrom,
            ValidTo = x.ValidTo,
            IsNewUserOnly = x.IsNewUserOnly,
            IsAllStores = x.IsAllStores,
            PerUserLimit = x.PerUserLimit,
            IsEnabled = x.IsEnabled,
            Remark = x.Remark,
            ProductIds = scopes.TryGetValue(x.Id, out var productIds) ? productIds : [],
            StoreIds = storeScopes.TryGetValue(x.Id, out var storeIds) ? storeIds : [],
            CreatedAt = x.CreatedAt,
        }).ToList();

        return Ok(Success(new PagedResult<CouponTemplateListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("coupon-template.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveCouponTemplateRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = new CouponTemplate
        {
            Name = request.Name.Trim(),
            ImageAssetId = request.ImageAssetId,
            TemplateType = request.TemplateType,
            ValidPeriodType = request.ValidPeriodType,
            DiscountAmount = request.DiscountAmount,
            ThresholdAmount = request.ThresholdAmount,
            ValidDays = request.ValidDays,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsNewUserOnly = request.IsNewUserOnly,
            IsAllStores = request.IsAllStores,
            PerUserLimit = request.PerUserLimit,
            IsEnabled = request.IsEnabled,
            Remark = request.Remark?.Trim(),
        };

        dbContext.CouponTemplates.Add(entity);
        await dbContext.SaveChangesAsync();
        await SyncProductScopesAsync(entity.Id, request);
        await SyncStoreScopesAsync(entity.Id, request);
        return Ok(Success(entity.Id, "创建成功"));
    }

    [AdminPermissionAuthorize("coupon-template.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveCouponTemplateRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.CouponTemplates.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("券模板不存在"));
        }

        entity.Name = request.Name.Trim();
        entity.ImageAssetId = request.ImageAssetId;
        entity.TemplateType = request.TemplateType;
        entity.ValidPeriodType = request.ValidPeriodType;
        entity.DiscountAmount = request.DiscountAmount;
        entity.ThresholdAmount = request.ThresholdAmount;
        entity.ValidDays = request.ValidDays;
        entity.ValidFrom = request.ValidFrom;
        entity.ValidTo = request.ValidTo;
        entity.IsNewUserOnly = request.IsNewUserOnly;
        entity.IsAllStores = request.IsAllStores;
        entity.PerUserLimit = request.PerUserLimit;
        entity.IsEnabled = request.IsEnabled;
        entity.Remark = request.Remark?.Trim();

        await dbContext.SaveChangesAsync();
        await SyncProductScopesAsync(entity.Id, request);
        await SyncStoreScopesAsync(entity.Id, request);
        return Ok(Success(entity.Id, "更新成功"));
    }

    [AdminPermissionAuthorize("coupon-template.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.CouponTemplates.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("券模板不存在"));
        }

        var scopes = await dbContext.CouponTemplateProductScopes.Where(x => x.CouponTemplateId == id).ToListAsync();
        if (scopes.Count > 0)
        {
            dbContext.CouponTemplateProductScopes.RemoveRange(scopes);
        }

        var storeScopes = await dbContext.CouponTemplateStoreScopes.Where(x => x.CouponTemplateId == id).ToListAsync();
        if (storeScopes.Count > 0)
        {
            dbContext.CouponTemplateStoreScopes.RemoveRange(storeScopes);
        }

        dbContext.CouponTemplates.Remove(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(true, "删除成功"));
    }

    private async Task SyncProductScopesAsync(long couponTemplateId, SaveCouponTemplateRequest request)
    {
        var oldScopes = await dbContext.CouponTemplateProductScopes.Where(x => x.CouponTemplateId == couponTemplateId).ToListAsync();
        if (oldScopes.Count > 0)
        {
            dbContext.CouponTemplateProductScopes.RemoveRange(oldScopes);
        }

        if (request.TemplateType == CouponTemplateType.Product)
        {
            var productIds = request.ProductIds.Where(x => x > 0).Distinct().ToArray();
            if (productIds.Length > 0)
            {
                dbContext.CouponTemplateProductScopes.AddRange(productIds.Select(productId => new CouponTemplateProductScope
                {
                    CouponTemplateId = couponTemplateId,
                    ProductId = productId,
                }));
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task SyncStoreScopesAsync(long couponTemplateId, SaveCouponTemplateRequest request)
    {
        var oldScopes = await dbContext.CouponTemplateStoreScopes.Where(x => x.CouponTemplateId == couponTemplateId).ToListAsync();
        if (oldScopes.Count > 0)
        {
            dbContext.CouponTemplateStoreScopes.RemoveRange(oldScopes);
        }

        if (!request.IsAllStores)
        {
            var storeIds = request.StoreIds.Where(x => x > 0).Distinct().ToArray();
            if (storeIds.Length > 0)
            {
                dbContext.CouponTemplateStoreScopes.AddRange(storeIds.Select(storeId => new CouponTemplateStoreScope
                {
                    CouponTemplateId = couponTemplateId,
                    StoreId = storeId,
                }));
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static string? ValidateRequest(SaveCouponTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "券模板名称不能为空";
        }

        if (request.TemplateType == CouponTemplateType.Product && request.ProductIds.Where(x => x > 0).Distinct().Count() == 0)
        {
            return "指定商品券必须至少配置一个商品";
        }

        if (!request.IsAllStores && request.StoreIds.Where(x => x > 0).Distinct().Count() == 0)
        {
            return "指定门店可用时必须至少配置一个门店";
        }

        if (request.ValidPeriodType == CouponValidPeriodType.FixedDateRange && (!request.ValidFrom.HasValue || !request.ValidTo.HasValue))
        {
            return "固定日期范围券必须设置开始和结束时间";
        }

        if (request.ValidPeriodType == CouponValidPeriodType.AfterReceiveDays && (!request.ValidDays.HasValue || request.ValidDays <= 0))
        {
            return "按领取后天数生效的券必须设置有效天数";
        }

        return null;
    }

}

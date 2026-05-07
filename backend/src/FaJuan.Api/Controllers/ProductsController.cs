using System.Text.Json;
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
[AdminMenuAuthorize("/products")]
public class ProductsController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword) || x.ErpProductCode.Contains(keyword) || (x.ErpIsbnCode != null && x.ErpIsbnCode.Contains(keyword)));
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ErpProductCode,
                x.ErpIsbnCode,
                x.MainImageAssetId,
                x.DetailImageAssetIds,
                x.ErpOriginalPrice,
                x.SalePrice,
                x.StockQuantity,
                x.IsEnabled,
                x.ShowInMiniApp,
                x.DirectPurchaseValidPeriodType,
                x.DirectPurchaseValidDays,
                x.DirectPurchaseValidFrom,
                x.DirectPurchaseValidTo,
                x.CreatedAt,
            })
            .ToListAsync();

        var assetIds = items
            .SelectMany(x => ParseDetailImageAssetIds(x.DetailImageAssetIds).Concat(x.MainImageAssetId.HasValue ? [x.MainImageAssetId.Value] : []))
            .Distinct()
            .ToArray();

        var assetMap = assetIds.Length == 0
            ? new Dictionary<long, string>()
            : await dbContext.MediaAssets.AsNoTracking()
                .Where(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileUrl);

        var resultItems = items.Select(x =>
        {
            var detailAssetIds = ParseDetailImageAssetIds(x.DetailImageAssetIds);

            return new ProductListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                ErpProductCode = x.ErpProductCode,
                ErpIsbnCode = x.ErpIsbnCode,
                MainImageAssetId = x.MainImageAssetId,
                MainImageUrl = x.MainImageAssetId.HasValue && assetMap.TryGetValue(x.MainImageAssetId.Value, out var mainImageUrl) ? mainImageUrl : null,
                DetailImageAssetIds = detailAssetIds,
                DetailImageUrls = detailAssetIds.Where(assetMap.ContainsKey).Select(assetId => assetMap[assetId]).ToArray(),
                ErpOriginalPrice = x.ErpOriginalPrice,
                SalePrice = x.SalePrice,
                StockQuantity = x.StockQuantity,
                IsEnabled = x.IsEnabled,
                ShowInMiniApp = x.ShowInMiniApp,
                DirectPurchaseValidPeriodType = x.DirectPurchaseValidPeriodType,
                DirectPurchaseValidDays = x.DirectPurchaseValidDays,
                DirectPurchaseValidFrom = x.DirectPurchaseValidFrom,
                DirectPurchaseValidTo = x.DirectPurchaseValidTo,
                CreatedAt = x.CreatedAt,
            };
        }).ToList();

        return Ok(Success(new PagedResult<ProductListItemDto>
        {
            Items = resultItems,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("product.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveProductRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var normalizedCode = request.ErpProductCode.Trim();
        var exists = await dbContext.Products.AnyAsync(x => x.ErpProductCode == normalizedCode, cancellationToken);
        if (exists)
        {
            return BadRequest(Failure<long>("ERP 商品编码已存在"));
        }

        var entity = new Product
        {
            Name = request.Name.Trim(),
            ErpProductCode = normalizedCode,
            ErpIsbnCode = string.IsNullOrWhiteSpace(request.ErpIsbnCode) ? null : request.ErpIsbnCode.Trim(),
            MainImageAssetId = request.MainImageAssetId,
            DetailImageAssetIds = SerializeDetailImageAssetIds(request.DetailImageAssetIds),
            ErpOriginalPrice = request.ErpOriginalPrice,
            SalePrice = request.SalePrice,
            StockQuantity = request.StockQuantity,
            IsEnabled = request.IsEnabled,
            ShowInMiniApp = request.ShowInMiniApp,
            DirectPurchaseValidPeriodType = request.DirectPurchaseValidPeriodType,
            DirectPurchaseValidDays = request.DirectPurchaseValidPeriodType == CouponValidPeriodType.AfterReceiveDays ? request.DirectPurchaseValidDays : null,
            DirectPurchaseValidFrom = request.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange ? request.DirectPurchaseValidFrom?.Date : null,
            DirectPurchaseValidTo = request.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange ? request.DirectPurchaseValidTo?.Date : null,
        };

        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncDirectPurchaseCouponTemplateAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(Success(entity.Id, "创建成功"));
    }

    [AdminPermissionAuthorize("product.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveProductRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(Failure<long>("商品不存在", 404));
        }

        var normalizedCode = request.ErpProductCode.Trim();
        var duplicatedCode = await dbContext.Products.AnyAsync(x => x.Id != id && x.ErpProductCode == normalizedCode, cancellationToken);
        if (duplicatedCode)
        {
            return BadRequest(Failure<long>("ERP 商品编码已存在"));
        }

        entity.Name = request.Name.Trim();
        entity.ErpProductCode = normalizedCode;
        entity.ErpIsbnCode = string.IsNullOrWhiteSpace(request.ErpIsbnCode) ? null : request.ErpIsbnCode.Trim();
        entity.MainImageAssetId = request.MainImageAssetId;
        entity.DetailImageAssetIds = SerializeDetailImageAssetIds(request.DetailImageAssetIds);
        entity.ErpOriginalPrice = request.ErpOriginalPrice;
        entity.SalePrice = request.SalePrice;
        entity.StockQuantity = request.StockQuantity;
        entity.IsEnabled = request.IsEnabled;
        entity.ShowInMiniApp = request.ShowInMiniApp;
        entity.DirectPurchaseValidPeriodType = request.DirectPurchaseValidPeriodType;
        entity.DirectPurchaseValidDays = request.DirectPurchaseValidPeriodType == CouponValidPeriodType.AfterReceiveDays ? request.DirectPurchaseValidDays : null;
        entity.DirectPurchaseValidFrom = request.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange ? request.DirectPurchaseValidFrom?.Date : null;
        entity.DirectPurchaseValidTo = request.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange ? request.DirectPurchaseValidTo?.Date : null;

        await SyncDirectPurchaseErpCodeReferencesAsync(entity.Id, normalizedCode, cancellationToken);
        await SyncDirectPurchaseCouponTemplateAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(Success(entity.Id, "更新成功"));
    }

    [AdminPermissionAuthorize("product.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(Failure<bool>("商品不存在", 404));
        }

        var hasReferences = await dbContext.CouponOrders.AsNoTracking().AnyAsync(x => x.ProductId == id, cancellationToken)
            || await dbContext.UserCoupons.AsNoTracking().AnyAsync(x => x.BoundProductId == id, cancellationToken)
            || await dbContext.CouponTemplateProductScopes.AsNoTracking().AnyAsync(x => x.ProductId == id, cancellationToken);
        if (hasReferences)
        {
            return BadRequest(Failure<bool>("商品已有关联订单或券记录，不能删除"));
        }

        if (entity.DirectPurchaseCouponTemplateId.HasValue)
        {
            var directTemplate = await dbContext.CouponTemplates.FirstOrDefaultAsync(x => x.Id == entity.DirectPurchaseCouponTemplateId.Value, cancellationToken);
            if (directTemplate is not null)
            {
                var scopes = await dbContext.CouponTemplateProductScopes.Where(x => x.CouponTemplateId == directTemplate.Id).ToListAsync(cancellationToken);
                dbContext.CouponTemplateProductScopes.RemoveRange(scopes);
                dbContext.CouponTemplates.Remove(directTemplate);
            }
        }

        dbContext.Products.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(Success(true, "删除成功"));
    }

    private async Task SyncDirectPurchaseCouponTemplateAsync(Product product, CancellationToken cancellationToken)
    {
        CouponTemplate? template = null;
        if (product.DirectPurchaseCouponTemplateId.HasValue)
        {
            template = await dbContext.CouponTemplates.FirstOrDefaultAsync(x => x.Id == product.DirectPurchaseCouponTemplateId.Value, cancellationToken);
        }

        if (template is null)
        {
            template = new CouponTemplate
            {
                Name = product.Name,
                TemplateType = CouponTemplateType.Product,
                ValidPeriodType = product.DirectPurchaseValidPeriodType ?? CouponValidPeriodType.AfterReceiveDays,
                ValidDays = product.DirectPurchaseValidDays,
                ValidFrom = product.DirectPurchaseValidFrom,
                ValidTo = product.DirectPurchaseValidTo,
                IsNewUserOnly = false,
                IsAllStores = true,
                PerUserLimit = 0,
                IsEnabled = true,
                DistributionMode = CouponDistributionMode.PackOnly,
                SalePrice = null,
                Remark = "商品直购提货券",
                ImageAssetId = product.MainImageAssetId,
                IsSystemProductVoucher = true,
            };

            dbContext.CouponTemplates.Add(template);
            await dbContext.SaveChangesAsync(cancellationToken);
            product.DirectPurchaseCouponTemplateId = template.Id;
        }

        template.Name = product.Name;
        template.TemplateType = CouponTemplateType.Product;
        template.ValidPeriodType = product.DirectPurchaseValidPeriodType ?? CouponValidPeriodType.AfterReceiveDays;
        template.ValidDays = product.DirectPurchaseValidPeriodType == CouponValidPeriodType.AfterReceiveDays ? product.DirectPurchaseValidDays : null;
        template.ValidFrom = product.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange ? product.DirectPurchaseValidFrom?.Date : null;
        template.ValidTo = product.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange ? product.DirectPurchaseValidTo?.Date : null;
        template.DiscountAmount = null;
        template.ThresholdAmount = null;
        template.IsNewUserOnly = false;
        template.IsAllStores = true;
        template.PerUserLimit = 0;
        template.IsEnabled = true;
        template.DistributionMode = CouponDistributionMode.PackOnly;
        template.SalePrice = null;
        template.Remark = "商品直购提货券";
        template.ImageAssetId = product.MainImageAssetId;
        template.IsSystemProductVoucher = true;

        var existingScopes = await dbContext.CouponTemplateProductScopes.Where(x => x.CouponTemplateId == template.Id).ToListAsync(cancellationToken);
        dbContext.CouponTemplateProductScopes.RemoveRange(existingScopes.Where(x => x.ProductId != product.Id));
        if (!existingScopes.Any(x => x.ProductId == product.Id))
        {
            dbContext.CouponTemplateProductScopes.Add(new CouponTemplateProductScope
            {
                CouponTemplateId = template.Id,
                ProductId = product.Id,
            });
        }
    }

    private async Task SyncDirectPurchaseErpCodeReferencesAsync(long productId, string normalizedCode, CancellationToken cancellationToken)
    {
        var pendingOrders = await dbContext.CouponOrders
            .Where(x => x.ProductId == productId
                && x.SourceType == CouponSourceType.ProductDirectPurchase
                && x.Status == CouponOrderStatus.PendingPayment
                && x.ProductErpProductCodeSnapshot != normalizedCode)
            .ToListAsync(cancellationToken);

        foreach (var order in pendingOrders)
        {
            order.ProductErpProductCodeSnapshot = normalizedCode;
        }

        var activeCoupons = await dbContext.UserCoupons
            .Where(x => x.BoundProductId == productId
                && x.SourceType == CouponSourceType.ProductDirectPurchase
                && x.Status == UserCouponStatus.Unused
                && x.BoundErpProductCode != normalizedCode)
            .ToListAsync(cancellationToken);

        foreach (var coupon in activeCoupons)
        {
            coupon.BoundErpProductCode = normalizedCode;
        }
    }

    private static string? ValidateRequest(SaveProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "商品名称不能为空";
        }

        if (string.IsNullOrWhiteSpace(request.ErpProductCode))
        {
            return "ERP 商品编码不能为空";
        }

        if (!request.ErpOriginalPrice.HasValue)
        {
            return "ERP 售价不能为空";
        }

        if (!request.SalePrice.HasValue)
        {
            return "销售价格不能为空";
        }

        if (!request.DirectPurchaseValidPeriodType.HasValue)
        {
            return "直购提货券有效期不能为空";
        }

        if (request.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange)
        {
            if (!request.DirectPurchaseValidFrom.HasValue || !request.DirectPurchaseValidTo.HasValue)
            {
                return "固定有效期必须填写开始日期和结束日期";
            }

            if (request.DirectPurchaseValidTo.Value.Date < request.DirectPurchaseValidFrom.Value.Date)
            {
                return "固定有效期结束日期不能早于开始日期";
            }
        }

        if (request.DirectPurchaseValidPeriodType == CouponValidPeriodType.AfterReceiveDays)
        {
            if (!request.DirectPurchaseValidDays.HasValue || request.DirectPurchaseValidDays.Value <= 0)
            {
                return "购买后有效天数必须大于 0";
            }
        }

        if (request.MainImageAssetId.HasValue && request.MainImageAssetId.Value <= 0)
        {
            return "商品主图素材无效";
        }

        if (request.ErpOriginalPrice.HasValue && request.ErpOriginalPrice.Value < 0)
        {
            return "ERP 售价不能小于 0";
        }

        if (request.SalePrice.HasValue && request.SalePrice.Value < 0)
        {
            return "销售价格不能小于 0";
        }

        if (request.StockQuantity.HasValue && request.StockQuantity.Value < 0)
        {
            return "库存不能小于 0";
        }

        return null;
    }

    private static string? SerializeDetailImageAssetIds(IReadOnlyCollection<long>? values)
    {
        if (values is null)
        {
            return null;
        }

        var normalized = values
            .Where(x => x > 0)
            .Distinct()
            .ToArray();

        return normalized.Length == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    private static IReadOnlyCollection<long> ParseDetailImageAssetIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<long[]>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

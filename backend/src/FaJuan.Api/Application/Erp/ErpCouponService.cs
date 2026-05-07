using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Application.Erp;

public class ErpCouponService(AppDbContext dbContext)
{
    public async Task<ErpCouponPreviewDto> PreviewAsync(ErpCouponPreviewRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSiteCode = request.SiteCode.Trim();
        var normalizedCouponCode = request.CouponCode.Trim();
        var store = await dbContext.Stores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalizedSiteCode && x.IsEnabled, cancellationToken);

        if (store is null)
        {
            return new ErpCouponPreviewDto
            {
                SiteCode = normalizedSiteCode,
                CouponCode = normalizedCouponCode,
                CanWriteOff = false,
                Message = "门店不存在或已停用",
            };
        }

        var coupon = await dbContext.UserCoupons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CouponCode == normalizedCouponCode, cancellationToken);

        if (coupon is null)
        {
            return new ErpCouponPreviewDto
            {
                SiteCode = normalizedSiteCode,
                StoreId = store.Id,
                StoreName = store.Name,
                CouponCode = normalizedCouponCode,
                CanWriteOff = false,
                Message = "券不存在",
            };
        }

        var template = await dbContext.CouponTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == coupon.CouponTemplateId, cancellationToken);

        if (template is null)
        {
            return BuildPreview(store, coupon, null, [], false, "券模板不存在或已删除");
        }

        var productScope = coupon.SourceType == CouponSourceType.ProductDirectPurchase
            ? await BuildBoundProductScopeAsync(coupon, cancellationToken)
            : await LoadProductScopeAsync(template.Id, cancellationToken);

        var validationMessage = await ValidateCouponAsync(coupon, template, store.Id, persistExpiredStatus: false, cancellationToken);
        if (coupon.SourceType == CouponSourceType.ProductDirectPurchase && productScope.Count == 0)
        {
            validationMessage ??= "商品提货券缺少绑定商品";
        }
        else if (template.TemplateType == CouponTemplateType.Product && productScope.Count == 0)
        {
            validationMessage ??= "商品券未配置可兑换商品";
        }

        return BuildPreview(
            store,
            coupon,
            template,
            productScope,
            string.IsNullOrWhiteSpace(validationMessage),
            validationMessage ?? "可核销");
    }

    public async Task<ErpCouponWriteOffServiceResult> WriteOffAsync(ErpCouponWriteOffRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSiteCode = request.SiteCode.Trim();
        var normalizedCouponCode = request.CouponCode.Trim();

        var store = await dbContext.Stores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalizedSiteCode && x.IsEnabled, cancellationToken);
        if (store is null)
        {
            return ErpCouponWriteOffServiceResult.Fail("门店不存在或已停用");
        }

        var coupon = await dbContext.UserCoupons.FirstOrDefaultAsync(x => x.CouponCode == normalizedCouponCode, cancellationToken);
        if (coupon is null)
        {
            return ErpCouponWriteOffServiceResult.Fail("券不存在", 404);
        }

        var template = await dbContext.CouponTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == coupon.CouponTemplateId, cancellationToken);
        if (template is null)
        {
            return ErpCouponWriteOffServiceResult.Fail("券模板不存在或已删除");
        }

        var validationMessage = await ValidateCouponAsync(coupon, template, store.Id, persistExpiredStatus: true, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return ErpCouponWriteOffServiceResult.Fail(validationMessage);
        }

        ErpCouponProductOptionDto? selectedProduct = null;
        if (coupon.SourceType == CouponSourceType.ProductDirectPurchase)
        {
            if (string.IsNullOrWhiteSpace(request.SelectedProductCode))
            {
                return ErpCouponWriteOffServiceResult.Fail("商品提货券核销时必须提供商品编码");
            }

            if (!string.Equals(coupon.BoundErpProductCode, request.SelectedProductCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return ErpCouponWriteOffServiceResult.Fail("该券只能核销指定商品");
            }

            selectedProduct = (await BuildBoundProductScopeAsync(coupon, cancellationToken)).FirstOrDefault();
            if (selectedProduct is null)
            {
                return ErpCouponWriteOffServiceResult.Fail("商品提货券缺少绑定商品");
            }
        }
        else if (template.TemplateType == CouponTemplateType.Product)
        {
            var productScope = await LoadProductScopeAsync(template.Id, cancellationToken);
            if (productScope.Count == 0)
            {
                return ErpCouponWriteOffServiceResult.Fail("商品券未配置可兑换商品");
            }

            var normalizedProductCode = request.SelectedProductCode?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedProductCode))
            {
                if (productScope.Count == 1)
                {
                    selectedProduct = productScope[0];
                }
                else
                {
                    return ErpCouponWriteOffServiceResult.Fail("商品券核销时必须选择商品");
                }
            }
            else
            {
                selectedProduct = productScope.FirstOrDefault(x => string.Equals(x.ErpProductCode, normalizedProductCode, StringComparison.OrdinalIgnoreCase));
                if (selectedProduct is null)
                {
                    return ErpCouponWriteOffServiceResult.Fail("该商品不在券适用范围内");
                }
            }
        }

        coupon.Status = UserCouponStatus.Used;
        if (template.TemplateType == CouponTemplateType.Product)
        {
            coupon.FulfillmentStatus = CouponFulfillmentStatus.Fulfilled;
        }

        dbContext.CouponWriteOffRecords.Add(new CouponWriteOffRecord
        {
            UserCouponId = coupon.Id,
            CouponCode = coupon.CouponCode,
            StoreId = store.Id,
            ProductId = selectedProduct?.ProductId,
            OperatorName = request.OperatorName?.Trim(),
            DeviceCode = request.DeviceCode?.Trim(),
            WriteOffAt = DateTime.Now,
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ErpCouponWriteOffServiceResult.Fail("券已核销");
        }

        return ErpCouponWriteOffServiceResult.Ok(new ErpCouponWriteOffResultDto
        {
            UserCouponId = coupon.Id,
            CouponCode = coupon.CouponCode,
            AppUserId = coupon.AppUserId,
            CouponTemplateId = coupon.CouponTemplateId,
            CouponTemplateName = template.Name,
            SiteCode = store.Code,
            StoreId = store.Id,
            StoreName = store.Name,
            SettlementType = GetSettlementType(template.TemplateType),
            SelectedProductCode = selectedProduct?.ErpProductCode,
            SelectedProductName = selectedProduct?.ProductName,
            Message = "核销成功",
        });
    }

    private async Task<string?> ValidateCouponAsync(UserCoupon coupon, CouponTemplate template, long storeId, bool persistExpiredStatus, CancellationToken cancellationToken)
    {
        if (coupon.Status == UserCouponStatus.Used)
        {
            return "券已核销";
        }

        if (coupon.Status == UserCouponStatus.Voided)
        {
            return "券已作废";
        }

        if (coupon.Status == UserCouponStatus.Expired)
        {
            return "券已过期";
        }

        var now = DateTime.Now;
        var todayStart = now.Date;
        if (coupon.EffectiveAt > now)
        {
            return "券未到生效时间";
        }

        if (coupon.ExpireAt < todayStart)
        {
            if (persistExpiredStatus)
            {
                coupon.Status = UserCouponStatus.Expired;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return "券已过期";
        }

        if (!template.IsAllStores)
        {
            var storeInScope = await dbContext.CouponTemplateStoreScopes.AsNoTracking()
                .AnyAsync(x => x.CouponTemplateId == template.Id && x.StoreId == storeId, cancellationToken);
            if (!storeInScope)
            {
                return "当前门店不在该券适用范围内";
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<ErpCouponProductOptionDto>> LoadProductScopeAsync(long couponTemplateId, CancellationToken cancellationToken)
    {
        return await dbContext.CouponTemplateProductScopes.AsNoTracking()
            .Where(x => x.CouponTemplateId == couponTemplateId)
            .Join(
                dbContext.Products.AsNoTracking().Where(x => x.IsEnabled),
                scope => scope.ProductId,
                product => product.Id,
                (_, product) => new ErpCouponProductOptionDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ErpProductCode = product.ErpProductCode,
                    ErpPrice = product.ErpOriginalPrice,
                    CouponPrice = product.SalePrice,
                    SettlementPrice = product.SalePrice,
                })
            .OrderBy(x => x.ProductId)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ErpCouponProductOptionDto>> BuildBoundProductScopeAsync(UserCoupon coupon, CancellationToken cancellationToken)
    {
        if (!coupon.BoundProductId.HasValue || string.IsNullOrWhiteSpace(coupon.BoundErpProductCode))
        {
            return [];
        }

        var product = await dbContext.Products.AsNoTracking()
            .Where(x => x.Id == coupon.BoundProductId.Value)
            .Select(product => new ErpCouponProductOptionDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ErpProductCode = product.ErpProductCode,
                ErpPrice = product.ErpOriginalPrice,
                CouponPrice = product.SalePrice,
                SettlementPrice = product.SalePrice,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is not null)
        {
            return [product];
        }

        return
        [
            new ErpCouponProductOptionDto
            {
                ProductId = coupon.BoundProductId.Value,
                ProductName = coupon.BoundProductName ?? string.Empty,
                ErpProductCode = coupon.BoundErpProductCode,
                ErpPrice = null,
                CouponPrice = null,
                SettlementPrice = null,
            }
        ];
    }

    private static ErpCouponPreviewDto BuildPreview(
        Store store,
        UserCoupon coupon,
        CouponTemplate? template,
        IReadOnlyCollection<ErpCouponProductOptionDto> productScope,
        bool canWriteOff,
        string message)
    {
        return new ErpCouponPreviewDto
        {
            SiteCode = store.Code,
            StoreId = store.Id,
            StoreName = store.Name,
            CouponCode = coupon.CouponCode,
            UserCouponId = coupon.Id,
            AppUserId = coupon.AppUserId,
            CouponTemplateId = template?.Id,
            CouponTemplateName = template?.Name ?? coupon.BoundProductName ?? string.Empty,
            TemplateType = template is null ? null : (int)template.TemplateType,
            Status = (int)coupon.Status,
            SettlementType = template is null ? string.Empty : GetSettlementType(template.TemplateType),
            DiscountAmount = template?.DiscountAmount,
            ThresholdAmount = template?.ThresholdAmount,
            EffectiveAt = coupon.EffectiveAt,
            ExpireAt = coupon.ExpireAt,
            CanWriteOff = canWriteOff,
            Message = message,
            ProductScope = productScope,
        };
    }

    private static string GetSettlementType(CouponTemplateType templateType) =>
        templateType == CouponTemplateType.Product ? "product_redeem" : "discount";
}

public sealed class ErpCouponWriteOffServiceResult
{
    public bool Success { get; init; }
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public ErpCouponWriteOffResultDto? Data { get; init; }

    public static ErpCouponWriteOffServiceResult Ok(ErpCouponWriteOffResultDto data) =>
        new()
        {
            Success = true,
            Code = 200,
            Message = data.Message,
            Data = data,
        };

    public static ErpCouponWriteOffServiceResult Fail(string message, int code = 400) =>
        new()
        {
            Success = false,
            Code = code,
            Message = message,
        };
}

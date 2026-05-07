using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Application.Orders;
using FaJuan.Api.Application.UserCoupons;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.MiniApp;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace FaJuan.Api.Controllers;

[Route("api/miniapp")]
public class MiniAppController(
    AppDbContext dbContext,
    OrderPaymentService orderPaymentService,
    UserCouponGrantService userCouponGrantService,
    WeChatPayService weChatPayService,
    MiniAppThemeSettingsService miniAppThemeSettingsService) : ApiControllerBase
{
    [HttpGet("settings")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MiniAppThemeDto>>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await miniAppThemeSettingsService.GetAsync(cancellationToken);
        return Ok(Success(new MiniAppThemeDto
        {
            ThemeCode = settings.ThemeCode,
        }));
    }

    [HttpGet("home")]
    [MiniAppAuthorize(Optional = true)]
    public async Task<ActionResult<ApiResponse<MiniAppHomeDto>>> GetHome(CancellationToken cancellationToken)
    {
        var settings = await miniAppThemeSettingsService.GetAsync(cancellationToken);
        var bannerRows = await dbContext.Banners.AsNoTracking()
            .Where(x => x.IsEnabled)
            .Join(dbContext.MediaAssets.AsNoTracking(), x => x.ImageAssetId, x => x.Id,
                (banner, asset) => new
                {
                    banner.Id,
                    banner.Title,
                    asset.FileUrl,
                    banner.LinkUrl,
                    banner.Sort,
                })
            .OrderByDescending(x => x.Sort)
            .ThenBy(x => x.Id)
            .Take(6)
            .ToListAsync(cancellationToken);

        var banners = bannerRows
            .Select(x => new MiniAppBannerDto
            {
                Id = x.Id,
                Title = x.Title,
                ImageUrl = ToAbsoluteAssetUrl(x.FileUrl),
                LinkUrl = NormalizeMiniAppLinkUrl(x.LinkUrl),
                Sort = x.Sort,
            })
            .ToList();

        var featuredCouponPacks = await dbContext.CouponPacks.AsNoTracking()
            .Where(x => x.Status == CouponPackStatus.Enabled)
            .OrderByDescending(x => x.CreatedAt)
            .Take(6)
            .Select(x => new MiniAppCouponPackCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageUrl = string.Empty,
                SalePrice = x.SalePrice,
                PerUserLimit = x.PerUserLimit,
                Remark = x.Remark,
                SaleStartTime = x.SaleStartTime,
                SaleEndTime = x.SaleEndTime,
            })
            .ToListAsync(cancellationToken);

        await FillCouponPackImageUrlsAsync(featuredCouponPacks, cancellationToken);

        var recommendedProducts = await dbContext.Products.AsNoTracking()
            .Where(x => x.IsEnabled && x.ShowInMiniApp == true)
            .OrderByDescending(x => x.CreatedAt)
            .Take(4)
            .Select(x => new MiniAppProductCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ErpProductCode = x.ErpProductCode,
                ErpIsbnCode = x.ErpIsbnCode,
                MainImageUrl = string.Empty,
                SalePrice = x.SalePrice,
            })
            .ToListAsync(cancellationToken);

        await FillProductImageUrlsAsync(recommendedProducts, cancellationToken);

        var directCoupons = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.IsEnabled && !x.IsSystemProductVoucher && x.DistributionMode == CouponDistributionMode.FreeClaim)
            .OrderByDescending(x => x.IsNewUserOnly)
            .ThenByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => new MiniAppCouponTemplateCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageUrl = string.Empty,
                TemplateType = (int)x.TemplateType,
                DiscountAmount = x.DiscountAmount,
                ThresholdAmount = x.ThresholdAmount,
                IsNewUserOnly = x.IsNewUserOnly,
                IsAllStores = x.IsAllStores,
                ValidPeriodType = (int)x.ValidPeriodType,
                ValidDays = x.ValidDays,
                ValidFrom = x.ValidFrom,
                ValidTo = x.ValidTo,
                Remark = x.Remark,
            })
            .ToListAsync(cancellationToken);

        await FillCouponTemplateImageUrlsAsync(directCoupons, cancellationToken);

        var currentUserId = GetCurrentUserId();
        MiniAppUserSummaryDto? userSummary = null;
        if (currentUserId.HasValue && currentUserId.Value > 0)
        {
            var user = await dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == currentUserId.Value, cancellationToken);
            if (user is not null)
            {
                var now = DateTime.Now;
                var todayStart = now.Date;
                var unusedCount = await dbContext.UserCoupons.AsNoTracking()
                    .CountAsync(x => x.AppUserId == user.Id && x.Status == UserCouponStatus.Unused && x.ExpireAt >= todayStart, cancellationToken);

                userSummary = new MiniAppUserSummaryDto
                {
                    UserId = user.Id,
                    Nickname = user.Nickname,
                    IsNewUser = !await dbContext.UserCoupons.AsNoTracking().AnyAsync(x => x.AppUserId == user.Id, cancellationToken),
                    UnusedCouponCount = unusedCount,
                };
            }
        }

        return Ok(Success(new MiniAppHomeDto
        {
            Theme = new MiniAppThemeDto
            {
                ThemeCode = settings.ThemeCode,
            },
            Banners = banners,
            FeaturedCouponPacks = featuredCouponPacks,
            RecommendedProducts = recommendedProducts,
            DirectCoupons = directCoupons,
            UserSummary = userSummary,
        }));
    }

    [HttpGet("coupon-packs")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<MiniAppCouponPackCardDto>>>> GetCouponPacks([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CouponPacks.AsNoTracking().Where(x => x.Status == CouponPackStatus.Enabled);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x => x.Name.Contains(normalizedKeyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new MiniAppCouponPackCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageUrl = string.Empty,
                SalePrice = x.SalePrice,
                PerUserLimit = x.PerUserLimit,
                Remark = x.Remark,
                SaleStartTime = x.SaleStartTime,
                SaleEndTime = x.SaleEndTime,
            })
            .ToListAsync(cancellationToken);

        await FillCouponPackImageUrlsAsync(items, cancellationToken);

        return Ok(Success(new PagedResult<MiniAppCouponPackCardDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("mall")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MiniAppMallDto>>> GetMall(CancellationToken cancellationToken)
    {
        var packs = await dbContext.CouponPacks.AsNoTracking()
            .Where(x => x.Status == CouponPackStatus.Enabled)
            .OrderByDescending(x => x.CreatedAt)
            .Take(4)
            .Select(x => new MiniAppCouponPackCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageUrl = string.Empty,
                SalePrice = x.SalePrice,
                PerUserLimit = x.PerUserLimit,
                Remark = x.Remark,
                SaleStartTime = x.SaleStartTime,
                SaleEndTime = x.SaleEndTime,
            })
            .ToListAsync(cancellationToken);
        await FillCouponPackImageUrlsAsync(packs, cancellationToken);

        var saleCoupons = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.IsEnabled
                && !x.IsSystemProductVoucher
                && x.DistributionMode == CouponDistributionMode.PaidStandalone
                && x.SalePrice.HasValue
                && x.SalePrice.Value > 0)
            .OrderByDescending(x => x.CreatedAt)
            .Take(12)
            .Select(x => new MiniAppSaleCouponCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageUrl = string.Empty,
                TemplateType = (int)x.TemplateType,
                SalePrice = x.SalePrice ?? 0m,
                DiscountAmount = x.DiscountAmount,
                ThresholdAmount = x.ThresholdAmount,
                IsAllStores = x.IsAllStores,
                IsNewUserOnly = x.IsNewUserOnly,
                Remark = x.Remark,
                FulfillmentHint = x.TemplateType == CouponTemplateType.Product ? "支付成功后待 ERP 履约" : null,
            })
            .ToListAsync(cancellationToken);

        var saleCouponIds = saleCoupons.Select(x => x.Id).ToArray();
        var productSummaryMap = saleCouponIds.Length == 0
            ? new Dictionary<long, string>()
            : await dbContext.CouponTemplateProductScopes.AsNoTracking()
                .Where(x => saleCouponIds.Contains(x.CouponTemplateId))
                .Join(dbContext.Products.AsNoTracking(), scope => scope.ProductId, product => product.Id,
                    (scope, product) => new { scope.CouponTemplateId, product.Name })
                .GroupBy(x => x.CouponTemplateId)
                .Select(x => new { CouponTemplateId = x.Key, ProductSummary = string.Join(" / ", x.Select(y => y.Name).Distinct().Take(2)) })
                .ToDictionaryAsync(x => x.CouponTemplateId, x => x.ProductSummary, cancellationToken);

        await FillSaleCouponImageUrlsAsync(saleCoupons, cancellationToken);
        foreach (var item in saleCoupons)
        {
            if (productSummaryMap.TryGetValue(item.Id, out var summary))
            {
                item.ProductSummary = summary;
            }
        }

        var standaloneCoupons = saleCoupons.Where(x => x.TemplateType != (int)CouponTemplateType.Product).ToList();
        var productCoupons = saleCoupons.Where(x => x.TemplateType == (int)CouponTemplateType.Product).ToList();

        var products = await dbContext.Products.AsNoTracking()
            .Where(x => x.IsEnabled && x.ShowInMiniApp == true)
            .OrderByDescending(x => x.CreatedAt)
            .Take(4)
            .Select(x => new MiniAppProductCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ErpProductCode = x.ErpProductCode,
                ErpIsbnCode = x.ErpIsbnCode,
                MainImageUrl = string.Empty,
                ErpOriginalPrice = x.ErpOriginalPrice,
                SalePrice = x.SalePrice,
            })
            .ToListAsync(cancellationToken);
        await FillProductImageUrlsAsync(products, cancellationToken);

        return Ok(Success(new MiniAppMallDto
        {
            Packs = packs,
            StandaloneCoupons = standaloneCoupons,
            ProductCoupons = productCoupons,
            Products = products,
        }));
    }

    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<MiniAppProductCardDto>>>> GetProducts([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking().Where(x => x.IsEnabled && x.ShowInMiniApp == true);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x => x.Name.Contains(normalizedKeyword)
                || x.ErpProductCode.Contains(normalizedKeyword)
                || (x.ErpIsbnCode != null && x.ErpIsbnCode.Contains(normalizedKeyword)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new MiniAppProductCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ErpProductCode = x.ErpProductCode,
                ErpIsbnCode = x.ErpIsbnCode,
                MainImageUrl = string.Empty,
                ErpOriginalPrice = x.ErpOriginalPrice,
                SalePrice = x.SalePrice,
            })
            .ToListAsync(cancellationToken);

        await FillProductImageUrlsAsync(items, cancellationToken);

        return Ok(Success(new PagedResult<MiniAppProductCardDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("products/{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MiniAppProductDetailDto>>> GetProductDetail(long id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking()
            .Where(x => x.Id == id && x.IsEnabled && x.ShowInMiniApp == true)
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
                x.DirectPurchaseCouponTemplateId,
                x.DirectPurchaseValidPeriodType,
                x.DirectPurchaseValidDays,
                x.DirectPurchaseValidFrom,
                x.DirectPurchaseValidTo,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return NotFound(Failure<MiniAppProductDetailDto>("商品不存在或已下架", 404));
        }

        var detailAssetIds = ParseDetailImageAssetIds(product.DetailImageAssetIds);
        var assetIds = detailAssetIds
            .Concat(product.MainImageAssetId.HasValue ? [product.MainImageAssetId.Value] : [])
            .Distinct()
            .ToArray();

        var assetMap = assetIds.Length == 0
            ? new Dictionary<long, string>()
            : await dbContext.MediaAssets.AsNoTracking()
                .Where(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileUrl, cancellationToken);

        var relatedCoupons = await dbContext.CouponTemplateProductScopes.AsNoTracking()
            .Where(x => x.ProductId == id)
            .Join(
                dbContext.CouponTemplates.AsNoTracking().Where(x => x.IsEnabled
                    && !x.IsSystemProductVoucher
                    && (x.DistributionMode == CouponDistributionMode.FreeClaim
                        || (x.DistributionMode == CouponDistributionMode.PaidStandalone && x.SalePrice.HasValue && x.SalePrice.Value > 0))),
                scope => scope.CouponTemplateId,
                template => template.Id,
                (_, template) => new MiniAppCouponTemplateCardDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    ImageUrl = string.Empty,
                    DistributionMode = (int)template.DistributionMode,
                    SalePrice = template.SalePrice,
                    TemplateType = (int)template.TemplateType,
                    DiscountAmount = template.DiscountAmount,
                    ThresholdAmount = template.ThresholdAmount,
                    IsNewUserOnly = template.IsNewUserOnly,
                    IsAllStores = template.IsAllStores,
                    ValidPeriodType = (int)template.ValidPeriodType,
                    ValidDays = template.ValidDays,
                    ValidFrom = template.ValidFrom,
                    ValidTo = template.ValidTo,
                    Remark = template.Remark,
                })
            .ToListAsync(cancellationToken);

        await FillCouponTemplateImageUrlsAsync(relatedCoupons, cancellationToken);
        await FillCouponTemplateProductSummariesAsync(relatedCoupons, cancellationToken);

        var relatedCouponIds = relatedCoupons.Select(x => x.Id).ToArray();
        var recommendedCoupons = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.IsEnabled
                && !x.IsSystemProductVoucher
                && (x.DistributionMode == CouponDistributionMode.FreeClaim
                    || (x.DistributionMode == CouponDistributionMode.PaidStandalone && x.SalePrice.HasValue && x.SalePrice.Value > 0))
                && !x.IsNewUserOnly
                && !relatedCouponIds.Contains(x.Id))
            .OrderByDescending(x => x.DistributionMode == CouponDistributionMode.PaidStandalone)
            .ThenByDescending(x => x.TemplateType == CouponTemplateType.Product)
            .ThenByDescending(x => x.CreatedAt)
            .Take(4)
            .Select(template => new MiniAppCouponTemplateCardDto
            {
                Id = template.Id,
                Name = template.Name,
                ImageUrl = string.Empty,
                DistributionMode = (int)template.DistributionMode,
                SalePrice = template.SalePrice,
                TemplateType = (int)template.TemplateType,
                DiscountAmount = template.DiscountAmount,
                ThresholdAmount = template.ThresholdAmount,
                IsNewUserOnly = template.IsNewUserOnly,
                IsAllStores = template.IsAllStores,
                ValidPeriodType = (int)template.ValidPeriodType,
                ValidDays = template.ValidDays,
                ValidFrom = template.ValidFrom,
                ValidTo = template.ValidTo,
                Remark = template.Remark,
            })
            .ToListAsync(cancellationToken);

        await FillCouponTemplateImageUrlsAsync(recommendedCoupons, cancellationToken);
        await FillCouponTemplateProductSummariesAsync(recommendedCoupons, cancellationToken);

        var detail = new MiniAppProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            ErpProductCode = product.ErpProductCode,
            ErpIsbnCode = product.ErpIsbnCode,
            MainImageUrl = product.MainImageAssetId.HasValue && assetMap.TryGetValue(product.MainImageAssetId.Value, out var mainImageUrl)
                ? ToAbsoluteAssetUrl(mainImageUrl)
                : null,
            DetailImageUrls = detailAssetIds
                .Where(assetMap.ContainsKey)
                .Select(assetId => ToAbsoluteAssetUrl(assetMap[assetId]))
                .ToArray(),
            ErpOriginalPrice = product.ErpOriginalPrice,
            SalePrice = product.SalePrice,
            IsEnabled = true,
            CanDirectPurchase = product.SalePrice.HasValue
                && product.SalePrice.Value > 0
                && product.DirectPurchaseCouponTemplateId.HasValue
                && product.DirectPurchaseValidPeriodType.HasValue,
            DirectPurchaseValidPeriodType = product.DirectPurchaseValidPeriodType.HasValue ? (int)product.DirectPurchaseValidPeriodType.Value : null,
            DirectPurchaseValidDays = product.DirectPurchaseValidDays,
            DirectPurchaseValidFrom = product.DirectPurchaseValidFrom,
            DirectPurchaseValidTo = product.DirectPurchaseValidTo,
            DirectPurchaseValidityText = BuildProductDirectPurchaseValidityText(
                product.DirectPurchaseValidPeriodType,
                product.DirectPurchaseValidDays,
                product.DirectPurchaseValidFrom,
                product.DirectPurchaseValidTo),
            Remark = null,
            RelatedCoupons = relatedCoupons,
            RecommendedCoupons = recommendedCoupons,
        };

        return Ok(Success(detail));
    }

    [HttpGet("sale-coupons/{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MiniAppSaleCouponDetailDto>>> GetSaleCouponDetail(long id, CancellationToken cancellationToken)
    {
        var coupon = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.Id == id
                && x.IsEnabled
                && !x.IsSystemProductVoucher
                && x.DistributionMode == CouponDistributionMode.PaidStandalone
                && x.SalePrice.HasValue
                && x.SalePrice.Value > 0)
            .Select(x => new MiniAppSaleCouponDetailDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageUrl = string.Empty,
                TemplateType = (int)x.TemplateType,
                DistributionMode = (int)x.DistributionMode,
                SalePrice = x.SalePrice ?? 0m,
                ValidPeriodType = (int)x.ValidPeriodType,
                DiscountAmount = x.DiscountAmount,
                ThresholdAmount = x.ThresholdAmount,
                ValidDays = x.ValidDays,
                ValidFrom = x.ValidFrom,
                ValidTo = x.ValidTo,
                IsNewUserOnly = x.IsNewUserOnly,
                IsAllStores = x.IsAllStores,
                PerUserLimit = x.PerUserLimit,
                TemplateRemark = x.Remark,
                FulfillmentHint = x.TemplateType == CouponTemplateType.Product ? "支付成功后待 ERP 履约" : null,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (coupon is null)
        {
            return NotFound(Failure<MiniAppSaleCouponDetailDto>("售卖券不存在或已下架", 404));
        }

        await FillSaleCouponImageUrlsAsync([coupon], cancellationToken);
        if (coupon.TemplateType == (int)CouponTemplateType.Product)
        {
            coupon.ProductSummary = await dbContext.CouponTemplateProductScopes.AsNoTracking()
                .Where(x => x.CouponTemplateId == coupon.Id)
                .Join(dbContext.Products.AsNoTracking(), scope => scope.ProductId, product => product.Id,
                    (_, product) => product.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Ok(Success(coupon));
    }

        [HttpGet("coupon-packs/{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MiniAppCouponPackDetailDto>>> GetCouponPackDetail(long id, CancellationToken cancellationToken)
    {
        var pack = await dbContext.CouponPacks.AsNoTracking()
            .Where(x => x.Id == id && x.Status == CouponPackStatus.Enabled)
            .Select(x => new MiniAppCouponPackDetailDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageUrl = string.Empty,
                SalePrice = x.SalePrice,
                PerUserLimit = x.PerUserLimit,
                Remark = x.Remark,
                SaleStartTime = x.SaleStartTime,
                SaleEndTime = x.SaleEndTime,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (pack is null)
        {
            return NotFound(Failure<MiniAppCouponPackDetailDto>("券包不存在", 404));
        }

        await FillCouponPackDetailImageUrlAsync(pack, cancellationToken);

        var items = await dbContext.CouponPackItems.AsNoTracking()
            .Where(x => x.CouponPackId == id)
            .Join(dbContext.CouponTemplates.AsNoTracking(), item => item.CouponTemplateId, template => template.Id,
                (item, template) => new MiniAppCouponPackItemDto
                {
                    CouponTemplateId = template.Id,

                    CouponTemplateName = template.Name,
                    Quantity = item.Quantity,
                    TemplateType = (int)template.TemplateType,
                    DiscountAmount = template.DiscountAmount,
                    ThresholdAmount = template.ThresholdAmount,
                    IsNewUserOnly = template.IsNewUserOnly,
                    IsAllStores = template.IsAllStores,
                })
            .ToListAsync(cancellationToken);

        return Ok(Success(new MiniAppCouponPackDetailDto
        {
            Id = pack.Id,
            Name = pack.Name,
            ImageUrl = pack.ImageUrl,
            SalePrice = pack.SalePrice,
            PerUserLimit = pack.PerUserLimit,
            Remark = pack.Remark,
            SaleStartTime = pack.SaleStartTime,
            SaleEndTime = pack.SaleEndTime,
            Items = items,
        }));
    }

    [HttpGet("coupon-templates/{id:long}")]
    [MiniAppAuthorize(Optional = true)]
    public async Task<ActionResult<ApiResponse<MiniAppCouponTemplateDetailDto>>> GetCouponTemplateDetail(long id, CancellationToken cancellationToken)
    {
        var detail = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.Id == id && x.IsEnabled)
            .Select(x => new MiniAppCouponTemplateDetailDto
            {
                Id = x.Id,
                Name = x.Name,
                ImageUrl = string.Empty,
                TemplateType = (int)x.TemplateType,
                ValidPeriodType = (int)x.ValidPeriodType,
                DiscountAmount = x.DiscountAmount,
                ThresholdAmount = x.ThresholdAmount,
                ValidDays = x.ValidDays,
                ValidFrom = x.ValidFrom,
                ValidTo = x.ValidTo,
                IsNewUserOnly = x.IsNewUserOnly,
                IsAllStores = x.IsAllStores,
                PerUserLimit = x.PerUserLimit,
                TemplateRemark = x.Remark,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (detail is null)
        {
            return NotFound(Failure<MiniAppCouponTemplateDetailDto>("券模板不存在", 404));
        }

        await FillCouponTemplateDetailImageUrlAsync(detail, cancellationToken);

        var currentUserId = GetCurrentUserId();
        var claimedCount = 0;
        if (currentUserId.HasValue && currentUserId.Value > 0)
        {
            claimedCount = await dbContext.UserCoupons.AsNoTracking()
                .CountAsync(x => x.AppUserId == currentUserId.Value && x.CouponTemplateId == id, cancellationToken);
        }

        return Ok(Success(new MiniAppCouponTemplateDetailDto
        {
            Id = detail.Id,
            Name = detail.Name,
            ImageUrl = detail.ImageUrl,
            TemplateType = detail.TemplateType,
            ValidPeriodType = detail.ValidPeriodType,
            DiscountAmount = detail.DiscountAmount,
            ThresholdAmount = detail.ThresholdAmount,
            ValidDays = detail.ValidDays,
            ValidFrom = detail.ValidFrom,
            ValidTo = detail.ValidTo,
            IsNewUserOnly = detail.IsNewUserOnly,
            IsAllStores = detail.IsAllStores,
            PerUserLimit = detail.PerUserLimit,
            TemplateRemark = detail.TemplateRemark,
            ClaimedCount = claimedCount,
            CanClaim = detail.PerUserLimit <= 0 || claimedCount < detail.PerUserLimit,
        }));
    }

    [HttpPost("coupon-templates/{id:long}/claim")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<MiniAppClaimCouponResultDto>>> ClaimCouponTemplate(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MiniAppClaimCouponRequest? request, CancellationToken cancellationToken)
    {
        _ = request;
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized(Failure<MiniAppClaimCouponResultDto>("请先登录", 401));
        }

        var result = await userCouponGrantService.GrantAsync(id, new[]
        {
            new ManualGrantUserCouponInput
            {
                AppUserId = userId.Value,
                QuantityPerUser = 1,
            }
        });

        var item = result.Items.FirstOrDefault();
        if (item is null || !item.Success)
        {
            return BadRequest(Failure<MiniAppClaimCouponResultDto>(item?.Message ?? "棰嗗彇澶辫触"));
        }

        var coupon = await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.AppUserId == userId.Value && x.CouponTemplateId == id)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (coupon is null)
        {
            return BadRequest(Failure<MiniAppClaimCouponResultDto>("领取成功但未找到用户券"));
        }

        return Ok(Success(new MiniAppClaimCouponResultDto
        {
            UserCouponId = coupon.Id,
            CouponTemplateId = coupon.CouponTemplateId,
            CouponCode = coupon.CouponCode,
            EffectiveAt = coupon.EffectiveAt,
            ExpireAt = coupon.ExpireAt,
        }, "棰嗗彇鎴愬姛"));
    }

    [HttpGet("users/coupons")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<PagedResult<MiniAppUserCouponCardDto>>>> GetUserCoupons([FromQuery] int? status, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized(Failure<PagedResult<MiniAppUserCouponCardDto>>("请先登录", 401));
        }

        if (!await dbContext.AppUsers.AsNoTracking().AnyAsync(x => x.Id == userId.Value, cancellationToken))
        {
            return NotFound(Failure<PagedResult<MiniAppUserCouponCardDto>>("用户不存在", 404));
        }

        var now = DateTime.Now;
        var todayStart = now.Date;
        var query = dbContext.UserCoupons.AsNoTracking().Where(x => x.AppUserId == userId.Value);
        if (status.HasValue && status.Value > 0)
        {
            if (status.Value == (int)UserCouponStatus.Unused)
            {
                query = query.Where(x => x.Status == UserCouponStatus.Unused && x.ExpireAt >= todayStart);
            }
            else if (status.Value == (int)UserCouponStatus.Expired)
            {
                query = query.Where(x => x.Status == UserCouponStatus.Expired || (x.Status == UserCouponStatus.Unused && x.ExpireAt < todayStart));
            }
            else
            {
                query = query.Where(x => (int)x.Status == status.Value);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Join(dbContext.CouponTemplates.AsNoTracking(), userCoupon => userCoupon.CouponTemplateId, template => template.Id,
                (userCoupon, template) => new MiniAppUserCouponCardDto
                {
                    Id = userCoupon.Id,
                    CouponTemplateId = template.Id,
                    SourceType = (int)userCoupon.SourceType,
                    BoundProductId = userCoupon.BoundProductId,
                    BoundProductName = userCoupon.BoundProductName,
                    BoundErpProductCode = userCoupon.BoundErpProductCode,
                    CouponTemplateName = template.Name,
                    TemplateType = (int)template.TemplateType,
                    DiscountAmount = template.DiscountAmount,
                    ThresholdAmount = template.ThresholdAmount,
                    CouponCode = userCoupon.CouponCode,
                    Status = userCoupon.Status == UserCouponStatus.Unused && userCoupon.ExpireAt < todayStart
                        ? (int)UserCouponStatus.Expired
                        : (int)userCoupon.Status,
                    EffectiveAt = userCoupon.EffectiveAt,
                    ExpireAt = userCoupon.ExpireAt,
                    ReceivedAt = userCoupon.ReceivedAt,
                    IsAllStores = template.IsAllStores,
                    IsNewUserOnly = template.IsNewUserOnly,
                    ImageUrl = string.Empty,
                })
            .ToListAsync(cancellationToken);

        await FillUserCouponImageUrlsAsync(items, cancellationToken);

        return Ok(Success(new PagedResult<MiniAppUserCouponCardDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("users/coupons/{id:long}")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<MiniAppCouponDetailDto>>> GetUserCouponDetail(long id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized(Failure<MiniAppCouponDetailDto>("请先登录", 401));
        }

        var detail = await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.Id == id && x.AppUserId == userId.Value)
            .Join(dbContext.CouponTemplates.AsNoTracking(), userCoupon => userCoupon.CouponTemplateId, template => template.Id,
                (userCoupon, template) => new MiniAppCouponDetailDto
                {
                    Id = userCoupon.Id,
                    AppUserId = userCoupon.AppUserId,
                    CouponTemplateId = template.Id,
                    SourceType = (int)userCoupon.SourceType,
                    BoundProductId = userCoupon.BoundProductId,
                    BoundProductName = userCoupon.BoundProductName,
                    BoundErpProductCode = userCoupon.BoundErpProductCode,
                    CouponTemplateName = template.Name,
                    CouponCode = userCoupon.CouponCode,
                    TemplateType = (int)template.TemplateType,
                    ValidPeriodType = (int)template.ValidPeriodType,
                    DiscountAmount = template.DiscountAmount,
                    ThresholdAmount = template.ThresholdAmount,
                    ValidDays = template.ValidDays,
                    ValidFrom = template.ValidFrom,
                    ValidTo = template.ValidTo,
                    IsNewUserOnly = template.IsNewUserOnly,
                    IsAllStores = template.IsAllStores,
                    PerUserLimit = template.PerUserLimit,
                    TemplateRemark = template.Remark,
                    Status = userCoupon.Status == UserCouponStatus.Unused && userCoupon.ExpireAt < DateTime.Now.Date
                        ? (int)UserCouponStatus.Expired
                        : (int)userCoupon.Status,
                    EffectiveAt = userCoupon.EffectiveAt,
                    ExpireAt = userCoupon.ExpireAt,
                    ReceivedAt = userCoupon.ReceivedAt,
                    ImageUrl = string.Empty,
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (detail is null)
        {
            return NotFound(Failure<MiniAppCouponDetailDto>("用户券不存在", 404));
        }

        await FillCouponDetailImageUrlAsync(detail, cancellationToken);

        var writeOffRecords = await dbContext.CouponWriteOffRecords.AsNoTracking()
            .Where(x => x.UserCouponId == id)
            .GroupJoin(dbContext.Stores.AsNoTracking(), record => record.StoreId, store => store.Id, (record, stores) => new { record, stores })
            .SelectMany(x => x.stores.DefaultIfEmpty(), (x, store) => new MiniAppWriteOffRecordDto
            {
                Id = x.record.Id,
                StoreId = x.record.StoreId,
                StoreName = store != null ? store.Name : string.Empty,
                OperatorName = x.record.OperatorName,
                DeviceCode = x.record.DeviceCode,
                WriteOffAt = x.record.WriteOffAt,
            })
            .OrderByDescending(x => x.WriteOffAt)
            .ToListAsync(cancellationToken);

        detail = new MiniAppCouponDetailDto
        {
            Id = detail.Id,
            AppUserId = detail.AppUserId,
            CouponTemplateId = detail.CouponTemplateId,
            SourceType = detail.SourceType,
            BoundProductId = detail.BoundProductId,
            BoundProductName = detail.BoundProductName,
            BoundErpProductCode = detail.BoundErpProductCode,
            CouponTemplateName = detail.CouponTemplateName,
            CouponCode = detail.CouponCode,
            QrPayload = detail.QrPayload,
            TemplateType = detail.TemplateType,
            ValidPeriodType = detail.ValidPeriodType,
            DiscountAmount = detail.DiscountAmount,
            ThresholdAmount = detail.ThresholdAmount,
            ValidDays = detail.ValidDays,
            ValidFrom = detail.ValidFrom,
            ValidTo = detail.ValidTo,
            IsNewUserOnly = detail.IsNewUserOnly,
            IsAllStores = detail.IsAllStores,
            PerUserLimit = detail.PerUserLimit,
            TemplateRemark = detail.TemplateRemark,
            Status = detail.Status,
            EffectiveAt = detail.EffectiveAt,
            ExpireAt = detail.ExpireAt,
            ReceivedAt = detail.ReceivedAt,
            ImageUrl = detail.ImageUrl,
            WriteOffRecords = writeOffRecords,
        };

        return Ok(Success(detail));
    }

    [HttpGet("users/orders")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<PagedResult<MiniAppOrderCardDto>>>> GetUserOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized(Failure<PagedResult<MiniAppOrderCardDto>>("请先登录", 401));
        }

        if (!await dbContext.AppUsers.AsNoTracking().AnyAsync(x => x.Id == userId.Value, cancellationToken))
        {
            return NotFound(Failure<PagedResult<MiniAppOrderCardDto>>("用户不存在", 404));
        }

        var query = dbContext.CouponOrders.AsNoTracking().Where(x => x.AppUserId == userId.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(order => new MiniAppOrderCardDto
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                SourceType = (int)order.SourceType,
                CouponPackId = order.CouponPackId,
                CouponPackName = dbContext.CouponPacks.AsNoTracking()
                    .Where(pack => order.CouponPackId.HasValue && pack.Id == order.CouponPackId.Value)
                    .Select(pack => pack.Name)
                    .FirstOrDefault(),
                CouponTemplateId = order.CouponTemplateId,
                CouponTemplateName = dbContext.CouponTemplates.AsNoTracking()
                    .Where(template => order.CouponTemplateId.HasValue && template.Id == order.CouponTemplateId.Value)
                    .Select(template => template.Name)
                    .FirstOrDefault(),
                ProductId = order.ProductId,
                ProductName = order.ProductNameSnapshot,
                IsProductCoupon = dbContext.CouponTemplates.AsNoTracking()
                    .Where(template => order.CouponTemplateId.HasValue && template.Id == order.CouponTemplateId.Value)
                    .Any(template => template.TemplateType == CouponTemplateType.Product),
                FulfillmentStatusText = order.Status == CouponOrderStatus.PendingPayment
                    ? "待付款"
                    : !dbContext.UserCoupons.AsNoTracking().Any(coupon => coupon.CouponOrderId == order.Id)
                        ? "未发券"
                        : dbContext.UserCoupons.AsNoTracking().Any(coupon => coupon.CouponOrderId == order.Id && coupon.FulfillmentStatus == CouponFulfillmentStatus.PendingFulfillment)
                            ? "待履约 / 待 ERP 处理"
                            : "待使用",
                OrderAmount = order.OrderAmount,
                Status = (int)order.Status,
                PaidAt = order.PaidAt,
                PaymentNo = order.PaymentNo,
                CreatedAt = order.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(Success(new PagedResult<MiniAppOrderCardDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("users/writeoff-records")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<MiniAppWriteOffRecordListDto>>> GetUserWriteOffRecords(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized(Failure<MiniAppWriteOffRecordListDto>("请重新登录", 401));
        }

        if (!await dbContext.AppUsers.AsNoTracking().AnyAsync(x => x.Id == userId.Value, cancellationToken))
        {
            return NotFound(Failure<MiniAppWriteOffRecordListDto>("用户不存在", 404));
        }

        var now = DateTime.Now;
        var monthStart = now.AddDays(-30);

        var records = await dbContext.CouponWriteOffRecords.AsNoTracking()
            .Join(dbContext.UserCoupons.AsNoTracking(),
                record => record.UserCouponId,
                coupon => coupon.Id,
                (record, coupon) => new { record, coupon })
            .Where(x => x.coupon.AppUserId == userId.Value)
            .Join(dbContext.CouponTemplates.AsNoTracking(),
                x => x.coupon.CouponTemplateId,
                template => template.Id,
                (x, template) => new { x.record, x.coupon, template })
            .GroupJoin(dbContext.Stores.AsNoTracking(),
                x => x.record.StoreId,
                store => store.Id,
                (x, stores) => new { x.record, x.coupon, x.template, stores })
            .SelectMany(x => x.stores.DefaultIfEmpty(), (x, store) => new MiniAppWriteOffTimelineItemDto
            {
                Id = x.record.Id,
                Title = $"{x.template.Name}已核销",
                Time = x.record.WriteOffAt,
                Status = "鏍搁攢鎴愬姛",
                Store = store != null ? store.Name : string.Empty,
                Coupon = x.template.Name,
                VerifyNo = $"HX{x.record.WriteOffAt:yyyyMMdd}{x.record.Id:D4}",
                Channel = !string.IsNullOrWhiteSpace(x.record.DeviceCode) ? "闂ㄥ簵鎵爜" : "闂ㄥ簵鍙楃悊",
                Note = !string.IsNullOrWhiteSpace(x.record.OperatorName) ? $"经办：{x.record.OperatorName}" : "核销完成",
                Tag = x.template.TemplateType == CouponTemplateType.Product ? "商品权益" : "已使用",
            })
            .OrderByDescending(x => x.Time)
            .ToListAsync(cancellationToken);

        var unusedCouponCount = await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.AppUserId == userId.Value && x.Status == UserCouponStatus.Unused && x.ExpireAt >= DateTime.Now.Date)
            .CountAsync(cancellationToken);

        return Ok(Success(new MiniAppWriteOffRecordListDto
        {
            TotalWriteOffCount = records.Count,
            MonthWriteOffCount = records.Count(x => x.Time >= monthStart),
            UnusedCouponCount = unusedCouponCount,
            Items = records,
        }));
    }

    [HttpGet("orders")]
    [MiniAppAuthorize]
    public Task<ActionResult<ApiResponse<PagedResult<MiniAppOrderCardDto>>>> GetOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return GetUserOrders(pageIndex, pageSize, cancellationToken);
    }

    [HttpGet("users/orders/{id:long}")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<MiniAppOrderDetailDto>>> GetUserOrderDetail(long id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized(Failure<MiniAppOrderDetailDto>("请先登录", 401));
        }

        var order = await dbContext.CouponOrders.AsNoTracking()
            .Where(x => x.Id == id && x.AppUserId == userId.Value)
            .Select(couponOrder => new MiniAppOrderDetailDto
            {
                Id = couponOrder.Id,
                OrderNo = couponOrder.OrderNo,
                SourceType = (int)couponOrder.SourceType,
                AppUserId = couponOrder.AppUserId,
                CouponPackId = couponOrder.CouponPackId,
                CouponPackName = dbContext.CouponPacks.AsNoTracking()
                    .Where(pack => couponOrder.CouponPackId.HasValue && pack.Id == couponOrder.CouponPackId.Value)
                    .Select(pack => pack.Name)
                    .FirstOrDefault(),
                CouponTemplateId = couponOrder.CouponTemplateId,
                CouponTemplateName = dbContext.CouponTemplates.AsNoTracking()
                    .Where(template => couponOrder.CouponTemplateId.HasValue && template.Id == couponOrder.CouponTemplateId.Value)
                    .Select(template => template.Name)
                    .FirstOrDefault(),
                ProductId = couponOrder.ProductId,
                ProductName = couponOrder.ProductNameSnapshot,
                IsProductCoupon = dbContext.CouponTemplates.AsNoTracking()
                    .Where(template => couponOrder.CouponTemplateId.HasValue && template.Id == couponOrder.CouponTemplateId.Value)
                    .Any(template => template.TemplateType == CouponTemplateType.Product),
                FulfillmentStatusText = couponOrder.Status == CouponOrderStatus.PendingPayment
                    ? "待付款"
                    : !dbContext.UserCoupons.AsNoTracking().Any(coupon => coupon.CouponOrderId == couponOrder.Id)
                        ? "未发券"
                        : dbContext.UserCoupons.AsNoTracking().Any(coupon => coupon.CouponOrderId == couponOrder.Id && coupon.FulfillmentStatus == CouponFulfillmentStatus.PendingFulfillment)
                            ? "待履约 / 待 ERP 处理"
                            : "待使用",
                OrderAmount = couponOrder.OrderAmount,
                Status = (int)couponOrder.Status,
                PaidAt = couponOrder.PaidAt,
                PaymentNo = couponOrder.PaymentNo,
                CreatedAt = couponOrder.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return NotFound(Failure<MiniAppOrderDetailDto>("订单不存在", 404));
        }

        var grantedCoupons = await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.CouponOrderId == id)
            .Join(dbContext.CouponTemplates.AsNoTracking(), userCoupon => userCoupon.CouponTemplateId, template => template.Id,
                (userCoupon, template) => new MiniAppUserCouponCardDto
                {
                    Id = userCoupon.Id,
                    CouponTemplateId = template.Id,
                    SourceType = (int)userCoupon.SourceType,
                    BoundProductId = userCoupon.BoundProductId,
                    BoundProductName = userCoupon.BoundProductName,
                    BoundErpProductCode = userCoupon.BoundErpProductCode,
                    CouponTemplateName = template.Name,
                    TemplateType = (int)template.TemplateType,
                    DiscountAmount = template.DiscountAmount,
                    ThresholdAmount = template.ThresholdAmount,
                    CouponCode = userCoupon.CouponCode,
                    Status = (int)userCoupon.Status,
                    EffectiveAt = userCoupon.EffectiveAt,
                    ExpireAt = userCoupon.ExpireAt,
                    ReceivedAt = userCoupon.ReceivedAt,
                    IsAllStores = template.IsAllStores,
                    IsNewUserOnly = template.IsNewUserOnly,
                    ImageUrl = string.Empty,
                })
            .ToListAsync(cancellationToken);

        await FillUserCouponImageUrlsAsync(grantedCoupons, cancellationToken);

        return Ok(Success(new MiniAppOrderDetailDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            SourceType = order.SourceType,
            AppUserId = order.AppUserId,
            CouponPackId = order.CouponPackId,
            CouponPackName = order.CouponPackName,
            CouponTemplateId = order.CouponTemplateId,
            CouponTemplateName = order.CouponTemplateName,
            ProductId = order.ProductId,
            ProductName = order.ProductName,
            IsProductCoupon = order.IsProductCoupon,
            FulfillmentStatusText = order.FulfillmentStatusText,
            OrderAmount = order.OrderAmount,
            Status = order.Status,
            PaidAt = order.PaidAt,
            PaymentNo = order.PaymentNo,
            CreatedAt = order.CreatedAt,
            GrantedCoupons = grantedCoupons,
        }));
    }

    [HttpGet("orders/{id:long}")]
    [MiniAppAuthorize]
    public Task<ActionResult<ApiResponse<MiniAppOrderDetailDto>>> GetOrderDetail(long id, CancellationToken cancellationToken)
    {
        return GetUserOrderDetail(id, cancellationToken);
    }

    [HttpPost("orders")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<MiniAppCreateOrderResultDto>>> CreateOrder([FromBody] MiniAppCreateOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return BadRequest(Failure<MiniAppCreateOrderResultDto>("用户不能为空"));
        }

        var user = await dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound(Failure<MiniAppCreateOrderResultDto>("用户不存在", 404));
        }

        var selectedSourceCount = (request.CouponPackId.HasValue ? 1 : 0)
            + (request.CouponTemplateId.HasValue ? 1 : 0)
            + (request.ProductId.HasValue ? 1 : 0);
        if (selectedSourceCount != 1)
        {
            return BadRequest(Failure<MiniAppCreateOrderResultDto>("券包、单张券、商品必须三选一"));
        }

        var now = DateTime.Now;
        if (request.CouponPackId.HasValue)
        {
            var pack = await dbContext.CouponPacks.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.CouponPackId.Value && x.Status == CouponPackStatus.Enabled, cancellationToken);
            if (pack is null)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>("券包不存在或已下架"));
            }

            if (pack.SaleStartTime.HasValue && pack.SaleStartTime.Value > now)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>("券包未到开售时间"));
            }

            if (pack.SaleEndTime.HasValue && pack.SaleEndTime.Value < now)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>("券包已结束售卖"));
            }

            if (pack.PerUserLimit > 0)
            {
                var orderCount = await dbContext.CouponOrders.AsNoTracking()
                    .CountAsync(x => x.AppUserId == userId.Value
                        && x.CouponPackId == request.CouponPackId.Value
                        && x.Status != CouponOrderStatus.Closed,
                        cancellationToken);
                if (orderCount >= pack.PerUserLimit)
                {
                    return BadRequest(Failure<MiniAppCreateOrderResultDto>($"该券包每位用户限购 {pack.PerUserLimit} 份"));
                }
            }

            var entity = new CouponOrder
            {
                OrderNo = OrderNoGenerator.Create("CP"),
                AppUserId = userId.Value,
                SourceType = CouponSourceType.CouponPack,
                CouponPackId = request.CouponPackId.Value,
                CouponTemplateId = null,
                OrderAmount = pack.SalePrice,
                Status = CouponOrderStatus.PendingPayment,
            };

            dbContext.CouponOrders.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(Success(new MiniAppCreateOrderResultDto
            {
                OrderId = entity.Id,
                OrderNo = entity.OrderNo,
                SourceType = (int)entity.SourceType,
                CouponPackId = entity.CouponPackId,
                CouponPackName = pack.Name,
                CouponTemplateId = null,
                CouponTemplateName = null,
                ProductId = null,
                ProductName = null,
                IsProductCoupon = false,
                OrderAmount = entity.OrderAmount,
                Status = (int)entity.Status,
                CreatedAt = entity.CreatedAt,
            }, "下单成功"));
        }

        if (request.ProductId.HasValue)
        {
            var product = await dbContext.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ProductId.Value && x.IsEnabled && x.ShowInMiniApp == true, cancellationToken);
            if (product is null)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>("商品不存在或已下架"));
            }

            if (!product.SalePrice.HasValue || product.SalePrice.Value <= 0)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>("商品当前不可直接购买"));
            }

            if (!product.DirectPurchaseCouponTemplateId.HasValue || !product.DirectPurchaseValidPeriodType.HasValue)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>("商品未配置提货券有效期，暂不可直接购买"));
            }

            var validityOk = product.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange
                ? product.DirectPurchaseValidFrom.HasValue && product.DirectPurchaseValidTo.HasValue
                : product.DirectPurchaseValidDays.HasValue && product.DirectPurchaseValidDays.Value > 0;
            if (!validityOk)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>("商品未配置提货券有效期，暂不可直接购买"));
            }

            var template = await dbContext.CouponTemplates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == product.DirectPurchaseCouponTemplateId.Value && (x.IsEnabled || x.IsSystemProductVoucher), cancellationToken);
            if (template is null)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>("商品提货券模板不存在，暂不可购买"));
            }

            var entity = new CouponOrder
            {
                OrderNo = OrderNoGenerator.Create("CP"),
                AppUserId = userId.Value,
                SourceType = CouponSourceType.ProductDirectPurchase,
                CouponPackId = null,
                CouponTemplateId = template.Id,
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                ProductErpProductCodeSnapshot = product.ErpProductCode,
                OrderAmount = product.SalePrice.Value,
                Status = CouponOrderStatus.PendingPayment,
            };

            dbContext.CouponOrders.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(Success(new MiniAppCreateOrderResultDto
            {
                OrderId = entity.Id,
                OrderNo = entity.OrderNo,
                SourceType = (int)entity.SourceType,
                CouponPackId = null,
                CouponPackName = null,
                CouponTemplateId = entity.CouponTemplateId,
                CouponTemplateName = template.Name,
                ProductId = product.Id,
                ProductName = product.Name,
                IsProductCoupon = true,
                OrderAmount = entity.OrderAmount,
                Status = (int)entity.Status,
                CreatedAt = entity.CreatedAt,
            }, "下单成功"));
        }

        var couponTemplateId = request.CouponTemplateId;
        if (!couponTemplateId.HasValue)
        {
            return BadRequest(Failure<MiniAppCreateOrderResultDto>("售卖券参数不能为空"));
        }

        var templateForSale = await dbContext.CouponTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == couponTemplateId.Value
                && x.IsEnabled
                && !x.IsSystemProductVoucher
                && x.DistributionMode == CouponDistributionMode.PaidStandalone
                && x.SalePrice.HasValue
                && x.SalePrice.Value > 0, cancellationToken);
        if (templateForSale is null)
        {
            return BadRequest(Failure<MiniAppCreateOrderResultDto>("售卖券不存在、未启用或不可购买"));
        }

        if (templateForSale.PerUserLimit > 0)
        {
            var orderCount = await dbContext.CouponOrders.AsNoTracking()
                .CountAsync(x => x.AppUserId == userId.Value
                    && x.CouponTemplateId == couponTemplateId.Value
                    && x.Status != CouponOrderStatus.Closed,
                    cancellationToken);
            if (orderCount >= templateForSale.PerUserLimit)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>($"该券每位用户限购 {templateForSale.PerUserLimit} 份"));
            }
        }

        var standaloneEntity = new CouponOrder
        {
            OrderNo = OrderNoGenerator.Create("CP"),
            AppUserId = userId.Value,
            SourceType = CouponSourceType.CouponTemplate,
            CouponPackId = null,
            CouponTemplateId = couponTemplateId.Value,
            OrderAmount = templateForSale.SalePrice!.Value,
            Status = CouponOrderStatus.PendingPayment,
        };

        dbContext.CouponOrders.Add(standaloneEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(Success(new MiniAppCreateOrderResultDto
        {
            OrderId = standaloneEntity.Id,
            OrderNo = standaloneEntity.OrderNo,
            SourceType = (int)standaloneEntity.SourceType,
            CouponPackId = null,
            CouponPackName = null,
            CouponTemplateId = standaloneEntity.CouponTemplateId,
            CouponTemplateName = templateForSale.Name,
            ProductId = null,
            ProductName = null,
            IsProductCoupon = templateForSale.TemplateType == CouponTemplateType.Product,
            OrderAmount = standaloneEntity.OrderAmount,
            Status = (int)standaloneEntity.Status,
            CreatedAt = standaloneEntity.CreatedAt,
        }, "下单成功"));
    }

    [HttpPost("orders/{id:long}/pay")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<MiniAppCreateOrderPaymentResultDto>>> PayOrder(long id, [FromBody] MiniAppCreateOrderPaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized(Failure<MiniAppCreateOrderPaymentResultDto>("请先登录", 401));
        }

        var order = await dbContext.CouponOrders.FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == userId.Value, cancellationToken);
        if (order is null)
        {
            return NotFound(Failure<MiniAppCreateOrderPaymentResultDto>("订单不存在", 404));
        }

        var payDescription = order.SourceType switch
        {
            CouponSourceType.CouponPack => $"券包订单-{order.OrderNo}",
            CouponSourceType.ProductDirectPurchase => $"商品订单-{order.OrderNo}",
            _ => $"单张券订单-{order.OrderNo}",
        };

        if (order.Status == CouponOrderStatus.Paid)
        {
            return Ok(Success(new MiniAppCreateOrderPaymentResultDto
            {
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                OrderStatus = (int)order.Status,
                Paid = true,
                Message = "订单已支付",
                Payment = new CreatePaymentResultDto
                {
                    PaymentNo = order.PaymentNo ?? string.Empty,
                    Amount = order.OrderAmount,
                },
            }, "订单已支付"));
        }

        var user = await dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.AppUserId, cancellationToken);
        if (user is null)
        {
            return BadRequest(Failure<MiniAppCreateOrderPaymentResultDto>("订单所属用户不存在"));
        }

        var transaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(
            x => x.CouponOrderId == order.Id && x.Status == PaymentStatus.Pending,
            cancellationToken);

        if (transaction is null)
        {
            transaction = new PaymentTransaction
            {
                CouponOrderId = order.Id,
                PaymentNo = OrderNoGenerator.Create("PAY"),
                Amount = order.OrderAmount,
                Status = PaymentStatus.Pending,
            };
            dbContext.PaymentTransactions.Add(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var payStatus = await weChatPayService.GetStatusAsync(cancellationToken);
        if (!payStatus.IsConfigured)
        {
            if (!payStatus.EnableMockFallback)
            {
                return BadRequest(Failure<MiniAppCreateOrderPaymentResultDto>("微信支付未配置完成，且已关闭模拟支付回退"));
            }

            var markResult = await orderPaymentService.MarkOrderPaidAsync(transaction, transaction.PaymentNo, "miniapp-mock-pay");
            if (!markResult.Success)
            {
                return BadRequest(Failure<MiniAppCreateOrderPaymentResultDto>(markResult.Message));
            }

            return Ok(Success(new MiniAppCreateOrderPaymentResultDto
            {
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                OrderStatus = (int)CouponOrderStatus.Paid,
                Paid = true,
                Message = "当前环境已完成模拟支付",
                Payment = new CreatePaymentResultDto
                {
                    PaymentTransactionId = transaction.Id,
                    PaymentNo = transaction.PaymentNo,
                    Amount = transaction.Amount,
                    IsMock = true,
                    MockPayToken = $"mock-pay-{transaction.PaymentNo}",
                },
            }, "当前环境已完成模拟支付"));
        }

        if (string.IsNullOrWhiteSpace(user.MiniOpenId))
        {
            return BadRequest(Failure<MiniAppCreateOrderPaymentResultDto>("用户缺少小程序 OpenId，无法发起 JSAPI 支付"));
        }

        var payResult = await weChatPayService.CreateJsapiOrderAsync(
            transaction.PaymentNo,
            payDescription,
            transaction.Amount,
            user.MiniOpenId,
            cancellationToken);

        if (!payResult.Success || payResult.Result is null)
        {
            return BadRequest(Failure<MiniAppCreateOrderPaymentResultDto>(payResult.Message));
        }

        var paymentResult = new CreatePaymentResultDto
        {
            PaymentTransactionId = transaction.Id,
            PaymentNo = payResult.Result.PaymentNo,
            Amount = payResult.Result.Amount,
            IsMock = payResult.Result.IsMock,
            MockPayToken = payResult.Result.MockPayToken,
            PrepayId = payResult.Result.PrepayId,
            TimeStamp = payResult.Result.TimeStamp,
            NonceStr = payResult.Result.NonceStr,
            PackageValue = payResult.Result.PackageValue,
            SignType = payResult.Result.SignType,
            PaySign = payResult.Result.PaySign,
        };

        return Ok(Success(new MiniAppCreateOrderPaymentResultDto
        {
            OrderId = order.Id,
            OrderNo = order.OrderNo,
            OrderStatus = (int)order.Status,
            Paid = false,
            Message = payResult.Message,
            Payment = paymentResult,
        }, payResult.Message));
    }

    [HttpPost("orders/{id:long}/complete-payment")]
    [MiniAppAuthorize]
    public async Task<ActionResult<ApiResponse<bool>>> CompleteOrderPayment(long id, [FromBody] MiniAppCompleteOrderPaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized(Failure<bool>("请先登录", 401));
        }

        var order = await dbContext.CouponOrders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == userId.Value, cancellationToken);
        if (order is null)
        {
            return NotFound(Failure<bool>("订单不存在", 404));
        }

        if (order.Status == CouponOrderStatus.Paid)
        {
            return Ok(Success(true, "支付已处理"));
        }

        var transaction = await dbContext.PaymentTransactions
            .FirstOrDefaultAsync(x => x.CouponOrderId == order.Id && x.Status == PaymentStatus.Pending, cancellationToken);
        if (transaction is null)
        {
            return Ok(Success(false, "未找到待确认支付流水"));
        }

        var queryResult = await weChatPayService.QueryTransactionByOutTradeNoAsync(transaction.PaymentNo, cancellationToken);
        if (!queryResult.Success || queryResult.Result is null)
        {
            return Ok(Success(false, queryResult.Message));
        }

        if (!string.Equals(queryResult.Result.TradeState, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(Success(false, $"微信支付状态：{queryResult.Result.TradeState ?? "未知"}"));
        }

        if (!string.Equals(queryResult.Result.OutTradeNo, transaction.PaymentNo, StringComparison.Ordinal))
        {
            return BadRequest(Failure<bool>("微信支付查单结果与本地支付流水不一致"));
        }

        var markResult = await orderPaymentService.MarkOrderPaidAsync(
            transaction,
            queryResult.Result.TransactionId,
            $"miniapp-query-order:{queryResult.Result.TradeState}");
        if (!markResult.Success)
        {
            return BadRequest(Failure<bool>(markResult.Message));
        }

        return Ok(Success(true, markResult.Message));
    }

    [HttpGet("users/coupons/{id:long}/qrcode")]
    [MiniAppAuthorize]
    public async Task<IActionResult> GetUserCouponQrCode(long id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized();
        }

        var coupon = await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.Id == id && x.AppUserId == userId.Value)
            .Select(x => new { x.CouponCode })
            .FirstOrDefaultAsync(cancellationToken);
        if (coupon is null)
        {
            return NotFound();
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(coupon.CouponCode, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(20);
        return File(bytes, "image/png");
    }

    private async Task FillCouponPackImageUrlsAsync(IReadOnlyCollection<MiniAppCouponPackCardDto> items, CancellationToken cancellationToken)
    {
        var packIds = items.Select(x => x.Id).ToArray();
        if (packIds.Length == 0)
        {
            return;
        }

        var imageMap = await dbContext.CouponPacks.AsNoTracking()
            .Where(x => packIds.Contains(x.Id) && x.ImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), pack => pack.ImageAssetId!.Value, asset => asset.Id, (pack, asset) => new { pack.Id, asset.FileUrl })
            .ToDictionaryAsync(x => x.Id, x => x.FileUrl, cancellationToken);

        foreach (var item in items)
        {
            if (imageMap.TryGetValue(item.Id, out var imageUrl))
            {
                item.ImageUrl = ToAbsoluteAssetUrl(imageUrl);
            }
        }
    }

    private async Task FillProductImageUrlsAsync(IReadOnlyCollection<MiniAppProductCardDto> items, CancellationToken cancellationToken)
    {
        var productIds = items.Select(x => x.Id).ToArray();
        if (productIds.Length == 0)
        {
            return;
        }

        var imageMap = await dbContext.Products.AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && x.MainImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), product => product.MainImageAssetId!.Value, asset => asset.Id, (product, asset) => new { product.Id, asset.FileUrl })
            .ToDictionaryAsync(x => x.Id, x => x.FileUrl, cancellationToken);

        foreach (var item in items)
        {
            if (imageMap.TryGetValue(item.Id, out var imageUrl))
            {
                item.MainImageUrl = ToAbsoluteAssetUrl(imageUrl);
            }
        }
    }

    private async Task FillSaleCouponImageUrlsAsync(IReadOnlyCollection<MiniAppSaleCouponCardDto> items, CancellationToken cancellationToken)
    {
        var couponIds = items.Select(x => x.Id).ToArray();
        if (couponIds.Length == 0)
        {
            return;
        }

        var imageMap = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => couponIds.Contains(x.Id) && x.ImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), coupon => coupon.ImageAssetId!.Value, asset => asset.Id, (coupon, asset) => new { coupon.Id, asset.FileUrl })
            .ToDictionaryAsync(x => x.Id, x => x.FileUrl, cancellationToken);

        foreach (var item in items)
        {
            if (imageMap.TryGetValue(item.Id, out var imageUrl))
            {
                item.ImageUrl = ToAbsoluteAssetUrl(imageUrl);
            }
        }
    }

    private async Task FillSaleCouponImageUrlsAsync(IReadOnlyCollection<MiniAppSaleCouponDetailDto> items, CancellationToken cancellationToken)
    {
        var couponIds = items.Select(x => x.Id).ToArray();
        if (couponIds.Length == 0)
        {
            return;
        }

        var imageMap = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => couponIds.Contains(x.Id) && x.ImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), coupon => coupon.ImageAssetId!.Value, asset => asset.Id, (coupon, asset) => new { coupon.Id, asset.FileUrl })
            .ToDictionaryAsync(x => x.Id, x => x.FileUrl, cancellationToken);

        foreach (var item in items)
        {
            if (imageMap.TryGetValue(item.Id, out var imageUrl))
            {
                item.ImageUrl = ToAbsoluteAssetUrl(imageUrl);
            }
        }
    }

    private static string? BuildDirectPurchaseValidityText(CouponValidPeriodType? periodType, int? validDays, DateTime? validFrom, DateTime? validTo)
    {
        if (!periodType.HasValue)
        {
            return null;
        }

        if (periodType == CouponValidPeriodType.FixedDateRange)
        {
            if (!validFrom.HasValue || !validTo.HasValue)
            {
                return null;
            }

            return $"有效期：{validFrom.Value:yyyy-MM-dd} 至 {validTo.Value:yyyy-MM-dd} 23:59:59";
        }

        if (!validDays.HasValue || validDays.Value <= 0)
        {
            return null;
        }

        return $"有效期：购买后 {validDays.Value} 天内有效（截止当日 23:59:59）";
    }

    private static IReadOnlyCollection<long> ParseDetailImageAssetIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<long[]>(value) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private async Task FillCouponTemplateDetailImageUrlAsync(MiniAppCouponTemplateDetailDto detail, CancellationToken cancellationToken)
    {
        var imageUrl = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.Id == detail.Id && x.ImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), template => template.ImageAssetId!.Value, asset => asset.Id, (template, asset) => asset.FileUrl)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            detail.ImageUrl = imageUrl;
        }
    }

    private async Task FillCouponTemplateImageUrlsAsync(IReadOnlyCollection<MiniAppCouponTemplateCardDto> items, CancellationToken cancellationToken)
    {
        var templateIds = items.Select(x => x.Id).ToArray();
        if (templateIds.Length == 0)
        {
            return;
        }

        var imageMap = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => templateIds.Contains(x.Id) && x.ImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), template => template.ImageAssetId!.Value, asset => asset.Id, (template, asset) => new { template.Id, asset.FileUrl })
            .ToDictionaryAsync(x => x.Id, x => x.FileUrl, cancellationToken);

        foreach (var item in items)
        {
            if (imageMap.TryGetValue(item.Id, out var imageUrl))
            {
                item.ImageUrl = ToAbsoluteAssetUrl(imageUrl);
            }
        }
    }

    private async Task FillCouponTemplateProductSummariesAsync(IReadOnlyCollection<MiniAppCouponTemplateCardDto> items, CancellationToken cancellationToken)
    {
        var templateIds = items.Select(x => x.Id).ToArray();
        if (templateIds.Length == 0)
        {
            return;
        }

        var productSummaryMap = await dbContext.CouponTemplateProductScopes.AsNoTracking()
            .Where(x => templateIds.Contains(x.CouponTemplateId))
            .Join(dbContext.Products.AsNoTracking(), scope => scope.ProductId, product => product.Id,
                (scope, product) => new { scope.CouponTemplateId, product.Name })
            .GroupBy(x => x.CouponTemplateId)
            .Select(x => new { CouponTemplateId = x.Key, ProductSummary = string.Join(" / ", x.Select(y => y.Name).Distinct().Take(2)) })
            .ToDictionaryAsync(x => x.CouponTemplateId, x => x.ProductSummary, cancellationToken);

        foreach (var item in items)
        {
            if (productSummaryMap.TryGetValue(item.Id, out var summary))
            {
                item.ProductSummary = summary;
            }
        }
    }

    private async Task FillUserCouponImageUrlsAsync(IReadOnlyCollection<MiniAppUserCouponCardDto> items, CancellationToken cancellationToken)
    {
        var templateIds = items.Select(x => x.CouponTemplateId).Distinct().ToArray();
        if (templateIds.Length == 0)
        {
            return;
        }

        var imageMap = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => templateIds.Contains(x.Id) && x.ImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), template => template.ImageAssetId!.Value, asset => asset.Id, (template, asset) => new { template.Id, asset.FileUrl })
            .ToDictionaryAsync(x => x.Id, x => x.FileUrl, cancellationToken);

        foreach (var item in items)
        {
            if (imageMap.TryGetValue(item.CouponTemplateId, out var imageUrl))
            {
                item.ImageUrl = imageUrl;
            }
        }
    }

    private async Task FillCouponDetailImageUrlAsync(MiniAppCouponDetailDto detail, CancellationToken cancellationToken)
    {
        var imageUrl = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.Id == detail.CouponTemplateId && x.ImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), template => template.ImageAssetId!.Value, asset => asset.Id, (template, asset) => asset.FileUrl)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            detail.ImageUrl = imageUrl;
        }
    }

    private async Task FillCouponPackDetailImageUrlAsync(MiniAppCouponPackDetailDto detail, CancellationToken cancellationToken)
    {
        var imageUrl = await dbContext.CouponPacks.AsNoTracking()
            .Where(x => x.Id == detail.Id && x.ImageAssetId.HasValue)
            .Join(dbContext.MediaAssets.AsNoTracking(), pack => pack.ImageAssetId!.Value, asset => asset.Id, (pack, asset) => asset.FileUrl)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            detail.ImageUrl = imageUrl;
        }
    }

    private static string? BuildProductDirectPurchaseValidityText(CouponValidPeriodType? validPeriodType, int? validDays, DateTime? validFrom, DateTime? validTo)
    {
        if (!validPeriodType.HasValue)
        {
            return null;
        }

        if (validPeriodType == CouponValidPeriodType.FixedDateRange)
        {
            if (!validFrom.HasValue || !validTo.HasValue)
            {
                return null;
            }

            return $"{validFrom.Value:yyyy-MM-dd} 至 {validTo.Value:yyyy-MM-dd}";
        }

        return validDays.HasValue && validDays.Value > 0
            ? $"购买后 {validDays.Value} 天内有效"
            : null;
    }
    private string ToAbsoluteAssetUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return value;
        }

        var request = HttpContext?.Request;
        if (request is null || !request.Host.HasValue)
        {
            return value;
        }

        var normalizedPath = value.StartsWith('/') ? value : $"/{value}";
        return $"{request.Scheme}://{request.Host}{normalizedPath}";
    }

    private static string? NormalizeMiniAppLinkUrl(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

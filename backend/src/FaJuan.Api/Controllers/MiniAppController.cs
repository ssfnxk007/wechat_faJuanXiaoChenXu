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
using Microsoft.Extensions.Options;
using QRCoder;

namespace FaJuan.Api.Controllers;

[Route("api/miniapp")]
public class MiniAppController(
    AppDbContext dbContext,
    OrderPaymentService orderPaymentService,
    UserCouponGrantService userCouponGrantService,
    WeChatPayService weChatPayService,
    IOptions<WeChatPayOptions> weChatPayOptions,
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
            .Where(x => x.IsEnabled)
            .OrderByDescending(x => x.CreatedAt)
            .Take(4)
            .Select(x => new MiniAppProductCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ErpProductCode = x.ErpProductCode,
                MainImageUrl = string.Empty,
                SalePrice = x.SalePrice,
            })
            .ToListAsync(cancellationToken);

        await FillProductImageUrlsAsync(recommendedProducts, cancellationToken);

        var directCoupons = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.IsEnabled)
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
                var unusedCount = await dbContext.UserCoupons.AsNoTracking()
                    .CountAsync(x => x.AppUserId == user.Id && x.Status == UserCouponStatus.Unused && x.ExpireAt >= now, cancellationToken);

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

    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<MiniAppProductCardDto>>>> GetProducts([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking().Where(x => x.IsEnabled);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x => x.Name.Contains(normalizedKeyword) || x.ErpProductCode.Contains(normalizedKeyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new MiniAppProductCardDto
            {
                Id = x.Id,
                Name = x.Name,
                ErpProductCode = x.ErpProductCode,
                MainImageUrl = string.Empty,
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
            .Where(x => x.Id == id && x.IsEnabled)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ErpProductCode,
                x.MainImageAssetId,
                x.DetailImageAssetIds,
                x.SalePrice,
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
                dbContext.CouponTemplates.AsNoTracking().Where(x => x.IsEnabled),
                scope => scope.CouponTemplateId,
                template => template.Id,
                (_, template) => new MiniAppCouponTemplateCardDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    ImageUrl = string.Empty,
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

        var relatedCouponIds = relatedCoupons.Select(x => x.Id).ToArray();
        var recommendedCoupons = await dbContext.CouponTemplates.AsNoTracking()
            .Where(x => x.IsEnabled
                && !x.IsNewUserOnly
                && (int)x.TemplateType != 3
                && !relatedCouponIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .Take(4)
            .Select(template => new MiniAppCouponTemplateCardDto
            {
                Id = template.Id,
                Name = template.Name,
                ImageUrl = string.Empty,
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

        var detail = new MiniAppProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            ErpProductCode = product.ErpProductCode,
            MainImageUrl = product.MainImageAssetId.HasValue && assetMap.TryGetValue(product.MainImageAssetId.Value, out var mainImageUrl)
                ? ToAbsoluteAssetUrl(mainImageUrl)
                : null,
            DetailImageUrls = detailAssetIds
                .Where(assetMap.ContainsKey)
                .Select(assetId => ToAbsoluteAssetUrl(assetMap[assetId]))
                .ToArray(),
            SalePrice = product.SalePrice,
            IsEnabled = true,
            Remark = null,
            RelatedCoupons = relatedCoupons,
            RecommendedCoupons = recommendedCoupons,
        };

        return Ok(Success(detail));
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
            return BadRequest(Failure<MiniAppClaimCouponResultDto>(item?.Message ?? "领取失败"));
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
        }, "领取成功"));
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

        var query = dbContext.UserCoupons.AsNoTracking().Where(x => x.AppUserId == userId.Value);
        if (status.HasValue && status.Value > 0)
        {
            query = query.Where(x => (int)x.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Join(dbContext.CouponTemplates.AsNoTracking(), userCoupon => userCoupon.CouponTemplateId, template => template.Id,
                (userCoupon, template) => new MiniAppUserCouponCardDto
                {
                    Id = userCoupon.Id,
                    CouponTemplateId = template.Id,
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
                    Status = (int)userCoupon.Status,
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
            .Join(dbContext.CouponPacks.AsNoTracking(), order => order.CouponPackId, pack => pack.Id,
                (order, pack) => new MiniAppOrderCardDto
                {
                    Id = order.Id,
                    OrderNo = order.OrderNo,
                    CouponPackId = order.CouponPackId,
                    CouponPackName = pack.Name,
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
            .Join(dbContext.CouponPacks.AsNoTracking(), couponOrder => couponOrder.CouponPackId, couponPack => couponPack.Id,
                (couponOrder, couponPack) => new MiniAppOrderDetailDto
                {
                    Id = couponOrder.Id,
                    OrderNo = couponOrder.OrderNo,
                    AppUserId = couponOrder.AppUserId,
                    CouponPackId = couponOrder.CouponPackId,
                    CouponPackName = couponPack.Name,
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
            AppUserId = order.AppUserId,
            CouponPackId = order.CouponPackId,
            CouponPackName = order.CouponPackName,
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
        if (!userId.HasValue || userId.Value <= 0 || request.CouponPackId <= 0)
        {
            return BadRequest(Failure<MiniAppCreateOrderResultDto>("用户与券包不能为空"));
        }

        var user = await dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound(Failure<MiniAppCreateOrderResultDto>("用户不存在", 404));
        }

        var pack = await dbContext.CouponPacks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CouponPackId && x.Status == CouponPackStatus.Enabled, cancellationToken);
        if (pack is null)
        {
            return BadRequest(Failure<MiniAppCreateOrderResultDto>("券包不存在或已下架"));
        }

        var now = DateTime.Now;
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
                    && x.CouponPackId == request.CouponPackId
                    && x.Status != CouponOrderStatus.Closed,
                    cancellationToken);
            if (orderCount >= pack.PerUserLimit)
            {
                return BadRequest(Failure<MiniAppCreateOrderResultDto>($"该券包每位用户限购 {pack.PerUserLimit} 份"));
            }
        }

        var entity = new CouponOrder
        {
            OrderNo = $"CP{now:yyyyMMddHHmmssfff}",
            AppUserId = userId.Value,
            CouponPackId = request.CouponPackId,
            OrderAmount = pack.SalePrice,
            Status = CouponOrderStatus.PendingPayment,
        };

        dbContext.CouponOrders.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(Success(new MiniAppCreateOrderResultDto
        {
            OrderId = entity.Id,
            OrderNo = entity.OrderNo,
            CouponPackId = entity.CouponPackId,
            CouponPackName = pack.Name,
            OrderAmount = entity.OrderAmount,
            Status = (int)entity.Status,
            CreatedAt = entity.CreatedAt,
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

        var packName = await dbContext.CouponPacks.AsNoTracking()
            .Where(x => x.Id == order.CouponPackId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

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
                PaymentNo = $"PAY{DateTime.Now:yyyyMMddHHmmssfff}",
                Amount = order.OrderAmount,
                Status = PaymentStatus.Pending,
            };
            dbContext.PaymentTransactions.Add(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!weChatPayService.IsConfigured())
        {
            if (!weChatPayOptions.Value.EnableMockFallback)
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
            $"券包订单-{order.OrderNo}",
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

        var transactionQuery = dbContext.PaymentTransactions.Where(x => x.CouponOrderId == id);
        if (!string.IsNullOrWhiteSpace(request.PaymentNo))
        {
            transactionQuery = transactionQuery.Where(x => x.PaymentNo == request.PaymentNo);
        }

        var transaction = await transactionQuery
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (transaction is null)
        {
            return NotFound(Failure<bool>("支付流水不存在", 404));
        }

        if (transaction.Status == PaymentStatus.Success)
        {
            return Ok(Success(true, "支付已处理"));
        }

        var result = await orderPaymentService.MarkOrderPaidAsync(transaction, request.ChannelTradeNo, request.RawCallback);
        if (!result.Success)
        {
            return BadRequest(Failure<bool>(result.Message));
        }

        return Ok(Success(true, result.Message));
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

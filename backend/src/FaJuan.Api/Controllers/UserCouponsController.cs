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
[AdminMenuAuthorize("/user-coupons")]
public class UserCouponsController(AppDbContext dbContext) : ApiControllerBase
{
    [AdminPermissionAuthorize("user-coupon.grant")]
    [HttpPost("manual-grant")]
    public async Task<ActionResult<ApiResponse<ManualGrantUserCouponsResultDto>>> ManualGrant([FromBody] ManualGrantUserCouponsRequest request)
    {
        if (request.CouponTemplateId <= 0)
        {
            return BadRequest(Failure<ManualGrantUserCouponsResultDto>("券模板不能为空"));
        }

        var appUserIds = request.AppUserIds.Where(x => x > 0).Distinct().ToArray();
        if (appUserIds.Length == 0)
        {
            return BadRequest(Failure<ManualGrantUserCouponsResultDto>("至少选择一个用户"));
        }

        if (request.QuantityPerUser <= 0)
        {
            return BadRequest(Failure<ManualGrantUserCouponsResultDto>("每人发放数量必须大于 0"));
        }

        var template = await dbContext.CouponTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CouponTemplateId);
        if (template is null)
        {
            return NotFound(Failure<ManualGrantUserCouponsResultDto>("券模板不存在", 404));
        }

        if (!template.IsEnabled)
        {
            return BadRequest(Failure<ManualGrantUserCouponsResultDto>("券模板已停用，不能发券"));
        }

        var users = await dbContext.AppUsers.AsNoTracking()
            .Where(x => appUserIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        var userIdSet = users.ToHashSet();
        var resultItems = new List<ManualGrantUserCouponItemDto>();

        foreach (var appUserId in appUserIds)
        {
            if (!userIdSet.Contains(appUserId))
            {
                resultItems.Add(new ManualGrantUserCouponItemDto
                {
                    AppUserId = appUserId,
                    Success = false,
                    GrantedCount = 0,
                    Message = "用户不存在"
                });
                continue;
            }

            var existingCount = await dbContext.UserCoupons.CountAsync(x => x.AppUserId == appUserId && x.CouponTemplateId == template.Id);
            if (existingCount + request.QuantityPerUser > template.PerUserLimit)
            {
                resultItems.Add(new ManualGrantUserCouponItemDto
                {
                    AppUserId = appUserId,
                    Success = false,
                    GrantedCount = 0,
                    Message = $"超过每人限领 {template.PerUserLimit} 张"
                });
                continue;
            }

            var now = DateTime.Now;
            var effectiveAt = template.ValidPeriodType == CouponValidPeriodType.FixedDateRange
                ? (template.ValidFrom ?? now)
                : now;
            var expireAt = template.ValidPeriodType == CouponValidPeriodType.FixedDateRange
                ? (template.ValidTo ?? now)
                : now.AddDays(template.ValidDays ?? 0);

            for (var index = 0; index < request.QuantityPerUser; index++)
            {
                dbContext.UserCoupons.Add(new UserCoupon
                {
                    AppUserId = appUserId,
                    CouponTemplateId = template.Id,
                    CouponCode = $"CPN{DateTime.Now:yyyyMMddHHmmssfff}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                    Status = UserCouponStatus.Unused,
                    ReceivedAt = now,
                    EffectiveAt = effectiveAt,
                    ExpireAt = expireAt,
                });
            }

            resultItems.Add(new ManualGrantUserCouponItemDto
            {
                AppUserId = appUserId,
                Success = true,
                GrantedCount = request.QuantityPerUser,
                Message = "发券成功"
            });
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }

        var result = new ManualGrantUserCouponsResultDto
        {
            CouponTemplateId = template.Id,
            SuccessCount = resultItems.Count(x => x.Success),
            FailureCount = resultItems.Count(x => !x.Success),
            Items = resultItems,
        };

        return Ok(Success(result, $"已处理 {resultItems.Count} 个用户"));
    }
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserCouponListItemDto>>>> GetList([FromQuery] long? userId, [FromQuery] string? couponCode, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.UserCoupons.AsNoTracking();

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(x => x.AppUserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            query = query.Where(x => x.CouponCode.Contains(couponCode));
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserCouponListItemDto
            {
                Id = x.Id,
                AppUserId = x.AppUserId,
                CouponTemplateId = x.CouponTemplateId,
                CouponCode = x.CouponCode,
                Status = (int)x.Status,
                EffectiveAt = x.EffectiveAt,
                ExpireAt = x.ExpireAt,
                ReceivedAt = x.ReceivedAt,
            })
            .ToListAsync();

        return Ok(Success(new PagedResult<UserCouponListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<UserCouponDetailDto>>> GetDetail(long id)
    {
        var detail = await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.Id == id)
            .Join(
                dbContext.CouponTemplates.AsNoTracking(),
                userCoupon => userCoupon.CouponTemplateId,
                template => template.Id,
                (userCoupon, template) => new UserCouponDetailDto
                {
                    Id = userCoupon.Id,
                    AppUserId = userCoupon.AppUserId,
                    CouponTemplateId = userCoupon.CouponTemplateId,
                    CouponTemplateName = template.Name,
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
                    TemplateEnabled = template.IsEnabled,
                    TemplateRemark = template.Remark,
                    CouponCode = userCoupon.CouponCode,
                    Status = (int)userCoupon.Status,
                    EffectiveAt = userCoupon.EffectiveAt,
                    ExpireAt = userCoupon.ExpireAt,
                    ReceivedAt = userCoupon.ReceivedAt,
                })
            .FirstOrDefaultAsync();

        if (detail is null)
        {
            return NotFound(Failure<UserCouponDetailDto>("用户券不存在"));
        }

        return Ok(Success(detail));
    }

    [HttpGet("{id:long}/writeoff-records")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CouponWriteOffRecordDto>>>> GetWriteOffRecords(long id)
    {
        var exists = await dbContext.UserCoupons.AsNoTracking().AnyAsync(x => x.Id == id);
        if (!exists)
        {
            return NotFound(Failure<IReadOnlyCollection<CouponWriteOffRecordDto>>("用户券不存在"));
        }

        var records = await dbContext.Set<Domain.Entities.CouponWriteOffRecord>().AsNoTracking()
            .Where(x => x.UserCouponId == id)
            .GroupJoin(
                dbContext.Stores.AsNoTracking(),
                record => record.StoreId,
                store => store.Id,
                (record, stores) => new { record, stores })
            .SelectMany(
                x => x.stores.DefaultIfEmpty(),
                (x, store) => new CouponWriteOffRecordDto
                {
                    Id = x.record.Id,
                    UserCouponId = x.record.UserCouponId,
                    CouponCode = x.record.CouponCode,
                    StoreId = x.record.StoreId,
                    StoreName = store != null ? store.Name : string.Empty,
                    OperatorName = x.record.OperatorName,
                    DeviceCode = x.record.DeviceCode,
                    WriteOffAt = x.record.WriteOffAt,
                    CreatedAt = x.record.CreatedAt,
                })
            .OrderByDescending(x => x.WriteOffAt)
            .ToListAsync();

        return Ok(Success<IReadOnlyCollection<CouponWriteOffRecordDto>>(records));
    }
}




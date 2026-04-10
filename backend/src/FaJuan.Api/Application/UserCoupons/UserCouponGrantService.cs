using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Application.UserCoupons;

public class UserCouponGrantService(AppDbContext dbContext)
{
    public async Task<ManualGrantUserCouponsResultDto> GrantAsync(long couponTemplateId, IReadOnlyCollection<ManualGrantUserCouponInput> inputs)
    {
        var normalizedInputs = inputs
            .Where(x => x.AppUserId > 0 && x.QuantityPerUser > 0)
            .GroupBy(x => x.AppUserId)
            .Select(group => new ManualGrantUserCouponInput
            {
                AppUserId = group.Key,
                QuantityPerUser = group.Sum(x => x.QuantityPerUser),
            })
            .ToArray();

        var template = await dbContext.CouponTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == couponTemplateId);
        if (template is null)
        {
            return new ManualGrantUserCouponsResultDto
            {
                CouponTemplateId = couponTemplateId,
                FailureCount = normalizedInputs.Length,
                Items = normalizedInputs.Select(x => new ManualGrantUserCouponItemDto
                {
                    AppUserId = x.AppUserId,
                    Success = false,
                    GrantedCount = 0,
                    Message = "券模板不存在",
                }).ToArray(),
            };
        }

        if (!template.IsEnabled)
        {
            return new ManualGrantUserCouponsResultDto
            {
                CouponTemplateId = couponTemplateId,
                FailureCount = normalizedInputs.Length,
                Items = normalizedInputs.Select(x => new ManualGrantUserCouponItemDto
                {
                    AppUserId = x.AppUserId,
                    Success = false,
                    GrantedCount = 0,
                    Message = "券模板已停用，不能发券",
                }).ToArray(),
            };
        }

        var appUserIds = normalizedInputs.Select(x => x.AppUserId).Distinct().ToHashSet();
        var users = (await dbContext.AppUsers.AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync())
            .Where(x => appUserIds.Contains(x))
            .ToList();

        var userIdSet = users.ToHashSet();
        var existingCounts = (await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.CouponTemplateId == template.Id)
            .GroupBy(x => x.AppUserId)
            .Select(group => new { AppUserId = group.Key, Count = group.Count() })
            .ToListAsync())
            .Where(x => appUserIds.Contains(x.AppUserId))
            .ToDictionary(x => x.AppUserId, x => x.Count);

        var now = DateTime.Now;
        var (effectiveAt, expireAt) = BuildCouponPeriod(template, now);

        var resultItems = new List<ManualGrantUserCouponItemDto>();

        foreach (var input in normalizedInputs)
        {
            if (!userIdSet.Contains(input.AppUserId))
            {
                resultItems.Add(new ManualGrantUserCouponItemDto
                {
                    AppUserId = input.AppUserId,
                    Success = false,
                    GrantedCount = 0,
                    Message = "用户不存在",
                });
                continue;
            }

            var existingCount = existingCounts.TryGetValue(input.AppUserId, out var count) ? count : 0;
            if (existingCount + input.QuantityPerUser > template.PerUserLimit)
            {
                resultItems.Add(new ManualGrantUserCouponItemDto
                {
                    AppUserId = input.AppUserId,
                    Success = false,
                    GrantedCount = 0,
                    Message = $"超过每人限领 {template.PerUserLimit} 张",
                });
                continue;
            }

            for (var index = 0; index < input.QuantityPerUser; index++)
            {
                dbContext.UserCoupons.Add(new UserCoupon
                {
                    AppUserId = input.AppUserId,
                    CouponTemplateId = template.Id,
                    CouponCode = GenerateCouponCode(),
                    Status = UserCouponStatus.Unused,
                    ReceivedAt = now,
                    EffectiveAt = effectiveAt,
                    ExpireAt = expireAt,
                });
            }

            resultItems.Add(new ManualGrantUserCouponItemDto
            {
                AppUserId = input.AppUserId,
                Success = true,
                GrantedCount = input.QuantityPerUser,
                Message = "发券成功",
            });
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }

        return new ManualGrantUserCouponsResultDto
        {
            CouponTemplateId = template.Id,
            SuccessCount = resultItems.Count(x => x.Success),
            FailureCount = resultItems.Count(x => !x.Success),
            Items = resultItems,
        };
    }

    private static (DateTime EffectiveAt, DateTime ExpireAt) BuildCouponPeriod(CouponTemplate template, DateTime now)
    {
        var effectiveAt = template.ValidPeriodType == CouponValidPeriodType.FixedDateRange
            ? (template.ValidFrom ?? now)
            : now;
        var expireAt = template.ValidPeriodType == CouponValidPeriodType.FixedDateRange
            ? (template.ValidTo ?? now)
            : now.AddDays(template.ValidDays ?? 0);

        return (effectiveAt, expireAt);
    }

    private static string GenerateCouponCode()
    {
        return $"CPN{DateTime.Now:yyyyMMddHHmmssfff}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }
}

using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Application.Common;

public static class CouponPeriodCalculator
{
    public static (DateTime EffectiveAt, DateTime ExpireAt) BuildCouponPeriod(CouponTemplate template, DateTime now)
    {
        if (template.ValidPeriodType == CouponValidPeriodType.FixedDateRange)
        {
            var effectiveAt = (template.ValidFrom ?? now).Date;
            var expireAt = EndOfDay(template.ValidTo ?? now);
            return (effectiveAt, expireAt);
        }

        var effective = now;
        var expire = EndOfDay(now.Date.AddDays(template.ValidDays ?? 0));
        return (effective, expire);
    }

    public static (DateTime EffectiveAt, DateTime ExpireAt) BuildProductDirectPurchasePeriod(Product product, DateTime now)
    {
        if (product.DirectPurchaseValidPeriodType == CouponValidPeriodType.FixedDateRange)
        {
            var effectiveAt = (product.DirectPurchaseValidFrom ?? now).Date;
            var expireAt = EndOfDay(product.DirectPurchaseValidTo ?? now);
            return (effectiveAt, expireAt);
        }

        var effective = now;
        var expire = EndOfDay(now.Date.AddDays(product.DirectPurchaseValidDays ?? 0));
        return (effective, expire);
    }

    private static DateTime EndOfDay(DateTime value)
    {
        return value.Date.AddDays(1).AddTicks(-1);
    }
}

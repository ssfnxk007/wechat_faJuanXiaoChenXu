using FaJuan.Api.Application.Common;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Application.Orders;

public class OrderPaymentService(AppDbContext dbContext)
{
    public async Task<(bool Success, string Message)> MarkOrderPaidAsync(PaymentTransaction transaction, string? channelTradeNo, string? rawCallback)
    {
        if (transaction.Status == PaymentStatus.Success)
        {
            return (true, "支付已处理");
        }

        var order = await dbContext.CouponOrders.FirstOrDefaultAsync(x => x.Id == transaction.CouponOrderId);
        if (order is null)
        {
            return (false, "订单不存在");
        }

        var packItems = await dbContext.CouponPackItems.AsNoTracking().Where(x => x.CouponPackId == order.CouponPackId).ToListAsync();
        if (packItems.Count == 0)
        {
            return (false, "券包未配置明细，无法发券");
        }

        transaction.Status = PaymentStatus.Success;
        transaction.ChannelTradeNo = channelTradeNo;
        transaction.RawCallback = rawCallback;
        transaction.PaidAt = DateTime.Now;

        order.Status = CouponOrderStatus.Paid;
        order.PaidAt = DateTime.Now;
        order.PaymentNo = transaction.PaymentNo;

        foreach (var item in packItems)
        {
            var template = await dbContext.CouponTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.CouponTemplateId);
            if (template is null)
            {
                continue;
            }

            for (var index = 0; index < item.Quantity; index++)
            {
                var now = DateTime.Now;
                var expireAt = template.ValidPeriodType == CouponValidPeriodType.FixedDateRange ? (template.ValidTo ?? now) : now.AddDays(template.ValidDays ?? 0);
                dbContext.UserCoupons.Add(new UserCoupon
                {
                    AppUserId = order.AppUserId,
                    CouponTemplateId = template.Id,
                    CouponOrderId = order.Id,
                    CouponCode = $"{OrderNoGenerator.Create("CPN")}{index:D2}",
                    Status = UserCouponStatus.Unused,
                    ReceivedAt = now,
                    EffectiveAt = template.ValidPeriodType == CouponValidPeriodType.FixedDateRange ? (template.ValidFrom ?? now) : now,
                    ExpireAt = expireAt,
                });
            }
        }

        await dbContext.SaveChangesAsync();
        return (true, "支付成功并已发券");
    }
}

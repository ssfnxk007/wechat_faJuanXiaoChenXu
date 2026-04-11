using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.ScheduledTasks.Jobs;

public class ExpireUnusedCouponsJob(AppDbContext dbContext)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var coupons = await dbContext.UserCoupons
            .Where(x => x.Status == UserCouponStatus.Unused && x.ExpireAt < now)
            .ToListAsync(cancellationToken);

        foreach (var coupon in coupons)
        {
            coupon.Status = UserCouponStatus.Expired;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.ScheduledTasks.Jobs;

public class CloseExpiredOrdersJob(AppDbContext dbContext)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.Now.AddMinutes(-30);
        var orders = await dbContext.CouponOrders
            .Where(x => x.Status == CouponOrderStatus.PendingPayment && x.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            order.Status = CouponOrderStatus.Closed;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

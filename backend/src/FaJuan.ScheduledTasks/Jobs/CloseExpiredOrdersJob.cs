using FaJuan.Api.Application.Orders;

namespace FaJuan.ScheduledTasks.Jobs;

public class CloseExpiredOrdersJob(OrderExpirationService orderExpirationService)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await orderExpirationService.CloseExpiredPendingOrdersAsync(cancellationToken);
    }
}

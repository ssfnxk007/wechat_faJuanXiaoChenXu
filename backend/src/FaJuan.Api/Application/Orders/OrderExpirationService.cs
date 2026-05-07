using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Application.Orders;

public class OrderExpirationService(
    AppDbContext dbContext,
    OrderPaymentService orderPaymentService,
    WeChatPayService weChatPayService)
{
    public const int PendingPaymentTimeoutMinutes = 30;

    public async Task<int> CloseExpiredPendingOrdersAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.Now.AddMinutes(-PendingPaymentTimeoutMinutes);
        var orders = await dbContext.CouponOrders
            .Where(x => x.Status == CouponOrderStatus.PendingPayment && x.CreatedAt <= cutoff)
            .OrderBy(x => x.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        var changedCount = 0;
        foreach (var order in orders)
        {
            var transaction = await dbContext.PaymentTransactions
                .Where(x => x.CouponOrderId == order.Id && x.Status == PaymentStatus.Pending)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (transaction is not null)
            {
                var paymentResolution = await ResolvePaymentBeforeClosingAsync(transaction, cancellationToken);
                if (paymentResolution == ExpiredPaymentResolution.Paid)
                {
                    changedCount++;
                    continue;
                }

                if (paymentResolution == ExpiredPaymentResolution.SkipClosing)
                {
                    continue;
                }
            }

            order.Status = CouponOrderStatus.Closed;
            if (transaction is not null)
            {
                transaction.Status = PaymentStatus.Failed;
                transaction.RawCallback = $"order-auto-closed:pending-payment-timeout-{PendingPaymentTimeoutMinutes}-minutes";
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            changedCount++;
        }

        return changedCount;
    }

    public bool IsExpiredPendingOrder(CouponOrderStatus status, DateTime createdAt)
    {
        return status == CouponOrderStatus.PendingPayment
            && createdAt <= DateTime.Now.AddMinutes(-PendingPaymentTimeoutMinutes);
    }

    private async Task<ExpiredPaymentResolution> ResolvePaymentBeforeClosingAsync(PaymentTransaction transaction, CancellationToken cancellationToken)
    {
        var payStatus = await weChatPayService.GetStatusAsync(cancellationToken);
        if (!payStatus.IsConfigured)
        {
            return ExpiredPaymentResolution.Close;
        }

        var queryResult = await weChatPayService.QueryTransactionByOutTradeNoAsync(transaction.PaymentNo, cancellationToken);
        if (!queryResult.Success || queryResult.Result is null)
        {
            return ExpiredPaymentResolution.SkipClosing;
        }

        if (!string.Equals(queryResult.Result.OutTradeNo, transaction.PaymentNo, StringComparison.Ordinal))
        {
            return ExpiredPaymentResolution.SkipClosing;
        }

        if (string.Equals(queryResult.Result.TradeState, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            await orderPaymentService.MarkOrderPaidAsync(
                transaction,
                queryResult.Result.TransactionId,
                $"order-expiration-query:{queryResult.Result.TradeState}");
            return ExpiredPaymentResolution.Paid;
        }

        if (string.Equals(queryResult.Result.TradeState, "USERPAYING", StringComparison.OrdinalIgnoreCase))
        {
            return ExpiredPaymentResolution.SkipClosing;
        }

        return ExpiredPaymentResolution.Close;
    }

    private enum ExpiredPaymentResolution
    {
        Close,
        SkipClosing,
        Paid,
    }
}

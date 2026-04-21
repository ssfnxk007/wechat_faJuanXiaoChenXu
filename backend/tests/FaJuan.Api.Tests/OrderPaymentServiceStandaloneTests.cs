using FaJuan.Api.Application.Orders;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Tests;

public class OrderPaymentServiceStandaloneTests
{
    [Fact]
    public async Task MarkOrderPaidAsync_Should_Grant_Single_Coupon_For_PaidStandalone_Template()
    {
        await using var dbContext = CreateDbContext();
        SeedStandaloneOrder(dbContext, templateType: CouponTemplateType.NoThreshold);
        var service = new OrderPaymentService(dbContext);
        var transaction = await dbContext.PaymentTransactions.FirstAsync();

        var result = await service.MarkOrderPaidAsync(transaction, "trade-standalone", "raw");

        Assert.True(result.Success);
        var order = await dbContext.CouponOrders.AsNoTracking().SingleAsync();
        var granted = await dbContext.UserCoupons.AsNoTracking().Where(x => x.CouponOrderId == order.Id).ToListAsync();
        Assert.Single(granted);
        Assert.Equal(CouponFulfillmentStatus.None, granted[0].FulfillmentStatus);
        Assert.Equal(UserCouponStatus.Unused, granted[0].Status);
    }

    [Fact]
    public async Task MarkOrderPaidAsync_Should_Grant_Product_Coupon_With_PendingFulfillment()
    {
        await using var dbContext = CreateDbContext();
        SeedStandaloneOrder(dbContext, templateType: CouponTemplateType.Product);
        var service = new OrderPaymentService(dbContext);
        var transaction = await dbContext.PaymentTransactions.FirstAsync();

        var result = await service.MarkOrderPaidAsync(transaction, "trade-product", "raw");

        Assert.True(result.Success);
        var granted = await dbContext.UserCoupons.AsNoTracking().ToListAsync();
        Assert.Single(granted);
        Assert.Equal(CouponFulfillmentStatus.PendingFulfillment, granted[0].FulfillmentStatus);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedStandaloneOrder(AppDbContext dbContext, CouponTemplateType templateType)
    {
        dbContext.CouponTemplates.Add(new CouponTemplate
        {
            Id = 100,
            Name = "阶段2单张券",
            TemplateType = templateType,
            ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            ValidDays = 7,
            DiscountAmount = 20m,
            IsEnabled = true,
            DistributionMode = CouponDistributionMode.PaidStandalone,
            SalePrice = 9.9m,
        });

        dbContext.CouponOrders.Add(new CouponOrder
        {
            Id = 10,
            OrderNo = "CP202604210001",
            AppUserId = 1,
            CouponTemplateId = 100,
            CouponPackId = null,
            OrderAmount = 9.9m,
            Status = CouponOrderStatus.PendingPayment,
        });

        dbContext.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = 20,
            CouponOrderId = 10,
            PaymentNo = "PAY202604210001",
            Amount = 9.9m,
            Status = PaymentStatus.Pending,
        });

        dbContext.SaveChanges();
    }
}

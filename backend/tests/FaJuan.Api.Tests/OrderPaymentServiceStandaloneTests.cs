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
        Assert.Equal(granted[0].ReceivedAt.Date.AddDays(7), granted[0].ExpireAt.Date);
        Assert.Equal(new TimeSpan(23, 59, 59), TruncateToSecond(granted[0].ExpireAt.TimeOfDay));
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

    [Fact]
    public async Task MarkOrderPaidAsync_Should_Use_End_Of_Day_For_Fixed_Date_Range_Template()
    {
        await using var dbContext = CreateDbContext();
        SeedStandaloneOrder(
            dbContext,
            templateType: CouponTemplateType.NoThreshold,
            validPeriodType: CouponValidPeriodType.FixedDateRange,
            validFrom: new DateTime(2026, 4, 22, 9, 0, 0),
            validTo: new DateTime(2026, 4, 29, 8, 0, 0));
        var service = new OrderPaymentService(dbContext);
        var transaction = await dbContext.PaymentTransactions.FirstAsync();

        var result = await service.MarkOrderPaidAsync(transaction, "trade-fixed", "raw");

        Assert.True(result.Success);
        var granted = await dbContext.UserCoupons.AsNoTracking().SingleAsync();
        Assert.Equal(new DateTime(2026, 4, 22), granted.EffectiveAt);
        Assert.Equal(new DateTime(2026, 4, 29), granted.ExpireAt.Date);
        Assert.Equal(new TimeSpan(23, 59, 59), TruncateToSecond(granted.ExpireAt.TimeOfDay));
    }

    [Fact]
    public async Task MarkOrderPaidAsync_Should_Grant_Bound_Product_Voucher_For_Direct_Product_Order()
    {
        await using var dbContext = CreateDbContext();
        SeedDirectProductOrder(dbContext);
        var service = new OrderPaymentService(dbContext);
        var transaction = await dbContext.PaymentTransactions.FirstAsync();

        var result = await service.MarkOrderPaidAsync(transaction, "trade-product-direct", "raw");

        Assert.True(result.Success);
        var granted = await dbContext.UserCoupons.AsNoTracking().SingleAsync();
        Assert.Equal(CouponSourceType.ProductDirectPurchase, granted.SourceType);
        Assert.Equal(30, granted.BoundProductId);
        Assert.Equal("超越训练", granted.BoundProductName);
        Assert.Equal("SKU-TRAIN-001", granted.BoundErpProductCode);
        Assert.Equal(CouponFulfillmentStatus.PendingFulfillment, granted.FulfillmentStatus);
        Assert.Equal(granted.ReceivedAt.Date.AddDays(7), granted.ExpireAt.Date);
        Assert.Equal(new TimeSpan(23, 59, 59), TruncateToSecond(granted.ExpireAt.TimeOfDay));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedStandaloneOrder(
        AppDbContext dbContext,
        CouponTemplateType templateType,
        CouponValidPeriodType validPeriodType = CouponValidPeriodType.AfterReceiveDays,
        DateTime? validFrom = null,
        DateTime? validTo = null)
    {
        dbContext.CouponTemplates.Add(new CouponTemplate
        {
            Id = 100,
            Name = "单张券测试",
            TemplateType = templateType,
            ValidPeriodType = validPeriodType,
            ValidDays = 7,
            ValidFrom = validFrom,
            ValidTo = validTo,
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
            SourceType = CouponSourceType.CouponTemplate,
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

    private static void SeedDirectProductOrder(AppDbContext dbContext)
    {
        dbContext.Products.Add(new Product
        {
            Id = 30,
            Name = "超越训练",
            ErpProductCode = "SKU-TRAIN-001",
            SalePrice = 88m,
            ErpOriginalPrice = 108m,
            IsEnabled = true,
            DirectPurchaseCouponTemplateId = 300,
            DirectPurchaseValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            DirectPurchaseValidDays = 7,
        });

        dbContext.CouponTemplates.Add(new CouponTemplate
        {
            Id = 300,
            Name = "超越训练",
            TemplateType = CouponTemplateType.Product,
            ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            ValidDays = 7,
            IsEnabled = true,
            IsAllStores = true,
            DistributionMode = CouponDistributionMode.PackOnly,
            IsSystemProductVoucher = true,
        });

        dbContext.CouponOrders.Add(new CouponOrder
        {
            Id = 10,
            OrderNo = "CP202604290001",
            AppUserId = 1,
            SourceType = CouponSourceType.ProductDirectPurchase,
            CouponTemplateId = 300,
            ProductId = 30,
            ProductNameSnapshot = "超越训练",
            ProductErpProductCodeSnapshot = "SKU-TRAIN-001",
            OrderAmount = 88m,
            Status = CouponOrderStatus.PendingPayment,
        });

        dbContext.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = 20,
            CouponOrderId = 10,
            PaymentNo = "PAY202604290001",
            Amount = 88m,
            Status = PaymentStatus.Pending,
        });

        dbContext.SaveChanges();
    }

    private static TimeSpan TruncateToSecond(TimeSpan value)
    {
        return TimeSpan.FromSeconds(Math.Floor(value.TotalSeconds));
    }
}

using FaJuan.Api.Contracts;
using FaJuan.Api.Controllers;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Tests;

public class ProductsControllerTests
{
    [Fact]
    public async Task Update_Should_Sync_Unused_DirectPurchase_Coupons_And_Pending_Orders_On_Save()
    {
        await using var db = CreateDbContext();
        SeedProductUpdateScenario(db);
        var controller = new ProductsController(db);

        var action = await controller.Update(30, new SaveProductRequest
        {
            Name = "PPPP",
            ErpProductCode = "03842608610196000000",
            ErpOriginalPrice = 68m,
            SalePrice = 66.8m,
            StockQuantity = null,
            IsEnabled = true,
            DirectPurchaseValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            DirectPurchaseValidDays = 7,
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<long>>(ok.Value);
        Assert.True(body.Success);

        var pendingOrder = await db.CouponOrders.AsNoTracking().SingleAsync(x => x.Id == 10);
        var paidOrder = await db.CouponOrders.AsNoTracking().SingleAsync(x => x.Id == 11);
        var unusedCoupon = await db.UserCoupons.AsNoTracking().SingleAsync(x => x.Id == 100);
        var usedCoupon = await db.UserCoupons.AsNoTracking().SingleAsync(x => x.Id == 101);

        Assert.Equal("03842608610196000000", pendingOrder.ProductErpProductCodeSnapshot);
        Assert.Equal("OLD-ERP-001", paidOrder.ProductErpProductCodeSnapshot);
        Assert.Equal("03842608610196000000", unusedCoupon.BoundErpProductCode);
        Assert.Equal("OLD-ERP-001", usedCoupon.BoundErpProductCode);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedProductUpdateScenario(AppDbContext db)
    {
        db.Products.Add(new Product
        {
            Id = 30,
            Name = "PPPP",
            ErpProductCode = "03842608610196000000",
            ErpOriginalPrice = 68m,
            SalePrice = 66.8m,
            IsEnabled = true,
            DirectPurchaseCouponTemplateId = 300,
            DirectPurchaseValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            DirectPurchaseValidDays = 7,
        });

        db.CouponTemplates.Add(new CouponTemplate
        {
            Id = 300,
            Name = "PPPP",
            TemplateType = CouponTemplateType.Product,
            ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            ValidDays = 7,
            IsEnabled = true,
            IsAllStores = true,
            DistributionMode = CouponDistributionMode.PackOnly,
            IsSystemProductVoucher = true,
        });

        db.CouponOrders.AddRange(
            new CouponOrder
            {
                Id = 10,
                OrderNo = "CP202604300010",
                AppUserId = 1,
                SourceType = CouponSourceType.ProductDirectPurchase,
                CouponTemplateId = 300,
                ProductId = 30,
                ProductNameSnapshot = "PPPP",
                ProductErpProductCodeSnapshot = "OLD-ERP-001",
                OrderAmount = 66.8m,
                Status = CouponOrderStatus.PendingPayment,
            },
            new CouponOrder
            {
                Id = 11,
                OrderNo = "CP202604300011",
                AppUserId = 1,
                SourceType = CouponSourceType.ProductDirectPurchase,
                CouponTemplateId = 300,
                ProductId = 30,
                ProductNameSnapshot = "PPPP",
                ProductErpProductCodeSnapshot = "OLD-ERP-001",
                OrderAmount = 66.8m,
                Status = CouponOrderStatus.Paid,
            });

        db.UserCoupons.AddRange(
            new UserCoupon
            {
                Id = 100,
                AppUserId = 1,
                CouponTemplateId = 300,
                CouponOrderId = 11,
                SourceType = CouponSourceType.ProductDirectPurchase,
                BoundProductId = 30,
                BoundProductName = "PPPP",
                BoundErpProductCode = "OLD-ERP-001",
                CouponCode = "CPN20260430001001",
                Status = UserCouponStatus.Unused,
                FulfillmentStatus = CouponFulfillmentStatus.PendingFulfillment,
                EffectiveAt = new DateTime(2026, 4, 30),
                ExpireAt = new DateTime(2026, 5, 7, 23, 59, 59),
            },
            new UserCoupon
            {
                Id = 101,
                AppUserId = 1,
                CouponTemplateId = 300,
                CouponOrderId = 11,
                SourceType = CouponSourceType.ProductDirectPurchase,
                BoundProductId = 30,
                BoundProductName = "PPPP",
                BoundErpProductCode = "OLD-ERP-001",
                CouponCode = "CPN20260430001002",
                Status = UserCouponStatus.Used,
                FulfillmentStatus = CouponFulfillmentStatus.Fulfilled,
                EffectiveAt = new DateTime(2026, 4, 30),
                ExpireAt = new DateTime(2026, 5, 7, 23, 59, 59),
            });

        db.SaveChanges();
    }
}

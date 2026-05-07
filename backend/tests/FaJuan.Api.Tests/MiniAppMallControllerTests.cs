using System.Security.Claims;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Application.Orders;
using FaJuan.Api.Application.UserCoupons;
using FaJuan.Api.Contracts;
using FaJuan.Api.Controllers;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.MiniApp;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Tests;

public class MiniAppMallControllerTests
{
    [Fact]
    public async Task GetMall_Should_Split_Standalone_And_Product_Coupons()
    {
        await using var db = CreateDbContext();
        SeedMallData(db);
        var controller = CreateController(db);

        var action = await controller.GetMall(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<MiniAppMallDto>>(ok.Value);
        Assert.Single(body.Data!.Packs);
        Assert.Single(body.Data.StandaloneCoupons);
        Assert.Single(body.Data.ProductCoupons);
        Assert.Single(body.Data.Products);
        Assert.Equal("单张售卖券", body.Data.StandaloneCoupons.First().Name);
        Assert.Equal("商品券", body.Data.ProductCoupons.First().Name);
        Assert.Equal("目标商品A", body.Data.ProductCoupons.First().ProductSummary);
    }

    [Fact]
    public async Task CreateOrder_Should_Reject_When_Pack_And_Template_Are_Both_Provided()
    {
        await using var db = CreateDbContext();
        SeedUser(db);
        var controller = CreateController(db, userId: 1);

        var action = await controller.CreateOrder(new MiniAppCreateOrderRequest
        {
            CouponPackId = 10,
            CouponTemplateId = 20,
        }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<MiniAppCreateOrderResultDto>>(bad.Value);
        Assert.Contains("三选一", body.Message);
    }

    [Fact]
    public async Task CreateOrder_Should_Create_Standalone_Coupon_Order()
    {
        await using var db = CreateDbContext();
        SeedUser(db);
        db.CouponTemplates.Add(new CouponTemplate
        {
            Id = 20,
            Name = "单张售卖券",
            TemplateType = CouponTemplateType.NoThreshold,
            ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            ValidDays = 7,
            DiscountAmount = 10,
            DistributionMode = CouponDistributionMode.PaidStandalone,
            SalePrice = 19.9m,
            IsEnabled = true,
            PerUserLimit = 2,
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId: 1);
        var action = await controller.CreateOrder(new MiniAppCreateOrderRequest
        {
            CouponTemplateId = 20,
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<MiniAppCreateOrderResultDto>>(ok.Value);
        Assert.Equal(20, body.Data!.CouponTemplateId);
        Assert.Null(body.Data.CouponPackId);
        Assert.False(body.Data.IsProductCoupon);
        Assert.Equal((int)CouponSourceType.CouponTemplate, body.Data.SourceType);

        var saved = await db.CouponOrders.AsNoTracking().SingleAsync();
        Assert.Equal(20, saved.CouponTemplateId);
        Assert.Null(saved.CouponPackId);
        Assert.Equal(CouponSourceType.CouponTemplate, saved.SourceType);
    }

    [Fact]
    public async Task CreateOrder_Should_Create_Direct_Product_Order()
    {
        await using var db = CreateDbContext();
        SeedUser(db);
        db.Products.Add(new Product
        {
            Id = 31,
            Name = "直购商品A",
            ErpProductCode = "SKU-DP-001",
            SalePrice = 88m,
            IsEnabled = true,
            DirectPurchaseCouponTemplateId = 201,
            DirectPurchaseValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            DirectPurchaseValidDays = 7,
        });
        db.CouponTemplates.Add(new CouponTemplate
        {
            Id = 201,
            Name = "直购商品A-商品提货券",
            TemplateType = CouponTemplateType.Product,
            ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            ValidDays = 7,
            IsEnabled = true,
            IsSystemProductVoucher = true,
            DistributionMode = CouponDistributionMode.PackOnly,
        });
        db.CouponTemplateProductScopes.Add(new CouponTemplateProductScope
        {
            CouponTemplateId = 201,
            ProductId = 31,
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId: 1);
        var action = await controller.CreateOrder(new MiniAppCreateOrderRequest
        {
            ProductId = 31,
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<MiniAppCreateOrderResultDto>>(ok.Value);
        Assert.Equal(31, body.Data!.ProductId);
        Assert.Equal(201, body.Data.CouponTemplateId);
        Assert.True(body.Data.IsProductCoupon);
        Assert.Equal((int)CouponSourceType.ProductDirectPurchase, body.Data.SourceType);

        var saved = await db.CouponOrders.AsNoTracking().SingleAsync();
        Assert.Equal(31, saved.ProductId);
        Assert.Equal(201, saved.CouponTemplateId);
        Assert.Equal(CouponSourceType.ProductDirectPurchase, saved.SourceType);
    }

    [Fact]
    public async Task GetUserCoupons_Should_Move_TimeExpired_Unused_Coupons_To_Expired_Tab()
    {
        await using var db = CreateDbContext();
        SeedUser(db);
        db.CouponTemplates.Add(new CouponTemplate
        {
            Id = 40,
            Name = "过期测试券",
            TemplateType = CouponTemplateType.NoThreshold,
            ValidPeriodType = CouponValidPeriodType.FixedDateRange,
            ValidFrom = DateTime.Today.AddDays(-10),
            ValidTo = DateTime.Today.AddDays(10),
            DiscountAmount = 10,
            DistributionMode = CouponDistributionMode.FreeClaim,
            IsEnabled = true,
            PerUserLimit = 1,
        });
        db.UserCoupons.AddRange(
            new UserCoupon
            {
                Id = 100,
                AppUserId = 1,
                CouponTemplateId = 40,
                CouponCode = "VALID-100",
                Status = UserCouponStatus.Unused,
                EffectiveAt = DateTime.Now.AddDays(-2),
                ExpireAt = DateTime.Now.AddDays(2),
                ReceivedAt = DateTime.Now.AddDays(-2),
            },
            new UserCoupon
            {
                Id = 101,
                AppUserId = 1,
                CouponTemplateId = 40,
                CouponCode = "EXPIRED-101",
                Status = UserCouponStatus.Unused,
                EffectiveAt = DateTime.Now.AddDays(-10),
                ExpireAt = DateTime.Now.AddDays(-1),
                ReceivedAt = DateTime.Now.AddDays(-10),
            },
            new UserCoupon
            {
                Id = 102,
                AppUserId = 1,
                CouponTemplateId = 40,
                CouponCode = "VALID-102",
                Status = UserCouponStatus.Unused,
                EffectiveAt = DateTime.Now.AddDays(-1),
                ExpireAt = DateTime.Now,
                ReceivedAt = DateTime.Now.AddDays(-1),
            });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId: 1);

        var availableAction = await controller.GetUserCoupons((int)UserCouponStatus.Unused, 1, 20, CancellationToken.None);
        var availableOk = Assert.IsType<OkObjectResult>(availableAction.Result);
        var availableBody = Assert.IsType<ApiResponse<PagedResult<MiniAppUserCouponCardDto>>>(availableOk.Value);
        Assert.Equal(2, availableBody.Data!.Items.Count);
        Assert.Contains(availableBody.Data.Items, item => item.Id == 100 && item.Status == (int)UserCouponStatus.Unused);
        Assert.Contains(availableBody.Data.Items, item => item.Id == 102 && item.Status == (int)UserCouponStatus.Unused);

        var expiredAction = await controller.GetUserCoupons((int)UserCouponStatus.Expired, 1, 20, CancellationToken.None);
        var expiredOk = Assert.IsType<OkObjectResult>(expiredAction.Result);
        var expiredBody = Assert.IsType<ApiResponse<PagedResult<MiniAppUserCouponCardDto>>>(expiredOk.Value);
        var expiredItem = Assert.Single(expiredBody.Data!.Items);
        Assert.Equal(101, expiredItem.Id);
        Assert.Equal((int)UserCouponStatus.Expired, expiredItem.Status);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static MiniAppController CreateController(AppDbContext db, long? userId = null)
    {
        var orderPaymentService = new OrderPaymentService(db);
        var userCouponGrantService = new UserCouponGrantService(db);
        var weChatPayService = new WeChatPayService(new HttpClient(), new WeChatPaySettingsProvider(db));
        var themeService = new MiniAppThemeSettingsService(new TestWebHostEnvironment(), Options.Create(new MiniAppThemeSettingsOptions()));
        var controller = new MiniAppController(db, orderPaymentService, userCouponGrantService, weChatPayService, themeService);

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("userId", userId.Value.ToString())
            ], "TestAuth"));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }

    private static void SeedMallData(AppDbContext db)
    {
        SeedUser(db);
        db.CouponPacks.Add(new CouponPack
        {
            Id = 10,
            Name = "券包A",
            SalePrice = 29.9m,
            PerUserLimit = 1,
            Status = CouponPackStatus.Enabled,
        });
        db.Products.Add(new Product
        {
            Id = 30,
            Name = "目标商品A",
            ErpProductCode = "SKU-001",
            SalePrice = 199m,
            IsEnabled = true,
        });
        db.CouponTemplates.AddRange(
            new CouponTemplate
            {
                Id = 20,
                Name = "单张售卖券",
                TemplateType = CouponTemplateType.NoThreshold,
                ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
                ValidDays = 7,
                DiscountAmount = 10,
                DistributionMode = CouponDistributionMode.PaidStandalone,
                SalePrice = 19.9m,
                IsEnabled = true,
                PerUserLimit = 2,
            },
            new CouponTemplate
            {
                Id = 21,
                Name = "商品券",
                TemplateType = CouponTemplateType.Product,
                ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
                ValidDays = 7,
                DiscountAmount = 20,
                DistributionMode = CouponDistributionMode.PaidStandalone,
                SalePrice = 39.9m,
                IsEnabled = true,
                PerUserLimit = 1,
            },
            new CouponTemplate
            {
                Id = 22,
                Name = "免费领券",
                TemplateType = CouponTemplateType.NoThreshold,
                ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
                ValidDays = 7,
                DiscountAmount = 5,
                DistributionMode = CouponDistributionMode.FreeClaim,
                SalePrice = null,
                IsEnabled = true,
                PerUserLimit = 1,
            });
        db.CouponTemplateProductScopes.Add(new CouponTemplateProductScope
        {
            CouponTemplateId = 21,
            ProductId = 30,
        });
        db.SaveChanges();
    }

    private static void SeedUser(AppDbContext db)
    {
        if (db.AppUsers.Any(x => x.Id == 1))
        {
            return;
        }

        db.AppUsers.Add(new AppUser
        {
            Id = 1,
            MiniOpenId = "mini-openid-1",
            CreatedAt = DateTime.Now,
        });
        db.SaveChanges();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FaJuan.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "FaJuan.Api.Tests.WebRoot");
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

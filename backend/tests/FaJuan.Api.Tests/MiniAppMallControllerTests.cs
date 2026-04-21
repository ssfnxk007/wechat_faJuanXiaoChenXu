using System.Security.Claims;
using FaJuan.Api.Application.Orders;
using FaJuan.Api.Application.UserCoupons;
using FaJuan.Api.Contracts;
using FaJuan.Api.Controllers;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.MiniApp;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
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
        Assert.Contains("二选一", body.Message);
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

        var saved = await db.CouponOrders.AsNoTracking().SingleAsync();
        Assert.Equal(20, saved.CouponTemplateId);
        Assert.Null(saved.CouponPackId);
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
            Nickname = "tester",
            MiniOpenId = "openid-test",
        });
        db.SaveChanges();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FaJuan.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "FaJuan.Api.Tests", Guid.NewGuid().ToString("N"));
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

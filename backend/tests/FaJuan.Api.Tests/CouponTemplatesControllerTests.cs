using FaJuan.Api.Contracts;
using FaJuan.Api.Controllers;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Tests;

public class CouponTemplatesControllerTests
{
    [Fact]
    public async Task Create_Should_Reject_PaidStandalone_Without_SalePrice()
    {
        await using var db = CreateDbContext();
        var controller = new CouponTemplatesController(db);

        var result = await controller.Create(BuildRequest(mode: CouponDistributionMode.PaidStandalone, salePrice: null));

        AssertBadRequestContains(result, "售价");
    }

    [Fact]
    public async Task Create_Should_Reject_PaidStandalone_With_Zero_SalePrice()
    {
        await using var db = CreateDbContext();
        var controller = new CouponTemplatesController(db);

        var result = await controller.Create(BuildRequest(mode: CouponDistributionMode.PaidStandalone, salePrice: 0m));

        AssertBadRequestContains(result, "售价");
    }

    [Fact]
    public async Task Create_Should_Force_SalePrice_Null_For_FreeClaim()
    {
        await using var db = CreateDbContext();
        var controller = new CouponTemplatesController(db);

        var action = await controller.Create(BuildRequest(mode: CouponDistributionMode.FreeClaim, salePrice: 9.9m));

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<long>>(ok.Value);
        var saved = db.CouponTemplates.AsNoTracking().Single(x => x.Id == body.Data);
        Assert.Null(saved.SalePrice);
        Assert.Equal(CouponDistributionMode.FreeClaim, saved.DistributionMode);
    }

    [Fact]
    public async Task Create_Should_Force_SalePrice_Null_For_PackOnly()
    {
        await using var db = CreateDbContext();
        var controller = new CouponTemplatesController(db);

        var action = await controller.Create(BuildRequest(mode: CouponDistributionMode.PackOnly, salePrice: 20m));

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<long>>(ok.Value);
        var saved = db.CouponTemplates.AsNoTracking().Single(x => x.Id == body.Data);
        Assert.Null(saved.SalePrice);
        Assert.Equal(CouponDistributionMode.PackOnly, saved.DistributionMode);
    }

    [Fact]
    public async Task Create_Should_Persist_SalePrice_For_PaidStandalone()
    {
        await using var db = CreateDbContext();
        var controller = new CouponTemplatesController(db);

        var action = await controller.Create(BuildRequest(mode: CouponDistributionMode.PaidStandalone, salePrice: 12.5m));

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<ApiResponse<long>>(ok.Value);
        var saved = db.CouponTemplates.AsNoTracking().Single(x => x.Id == body.Data);
        Assert.Equal(12.5m, saved.SalePrice);
        Assert.Equal(CouponDistributionMode.PaidStandalone, saved.DistributionMode);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static SaveCouponTemplateRequest BuildRequest(CouponDistributionMode mode, decimal? salePrice)
    {
        return new SaveCouponTemplateRequest
        {
            Name = "TestCoupon",
            TemplateType = CouponTemplateType.NoThreshold,
            ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            ValidDays = 7,
            IsAllStores = true,
            IsEnabled = true,
            PerUserLimit = 1,
            DistributionMode = mode,
            SalePrice = salePrice,
        };
    }

    private static void AssertBadRequestContains(ActionResult<ApiResponse<long>> actionResult, string expectedKeyword)
    {
        var bad = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var body = Assert.IsType<ApiResponse<long>>(bad.Value);
        Assert.False(body.Success);
        Assert.Contains(expectedKeyword, body.Message);
    }
}

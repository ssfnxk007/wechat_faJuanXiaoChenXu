using FaJuan.Api.Application.UserCoupons;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Tests;

public class UserCouponGrantServiceTests
{
    [Fact]
    public async Task GrantAsync_Should_Create_UserCoupons_For_Existing_User()
    {
        await using var dbContext = CreateDbContext();
        dbContext.AppUsers.Add(new AppUser { Id = 1, MiniOpenId = "mini-1", Mobile = "13800138000" });
        dbContext.CouponTemplates.Add(new CouponTemplate
        {
            Id = 10,
            Name = "测试券",
            TemplateType = CouponTemplateType.NoThreshold,
            ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            ValidDays = 7,
            PerUserLimit = 3,
            IsEnabled = true,
        });
        await dbContext.SaveChangesAsync();

        var service = new UserCouponGrantService(dbContext);

        var result = await service.GrantAsync(10, [new() { AppUserId = 1, QuantityPerUser = 2 }]);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Single(result.Items);
        Assert.True(result.Items.First().Success);
        Assert.Equal(2, result.Items.First().GrantedCount);
        Assert.Equal(2, await dbContext.UserCoupons.CountAsync(x => x.AppUserId == 1 && x.CouponTemplateId == 10));
    }

    [Fact]
    public async Task GrantAsync_Should_Fail_When_Exceed_PerUserLimit()
    {
        await using var dbContext = CreateDbContext();
        dbContext.AppUsers.Add(new AppUser { Id = 2, MiniOpenId = "mini-2", Mobile = "13900139000" });
        dbContext.CouponTemplates.Add(new CouponTemplate
        {
            Id = 20,
            Name = "限领券",
            TemplateType = CouponTemplateType.NoThreshold,
            ValidPeriodType = CouponValidPeriodType.AfterReceiveDays,
            ValidDays = 5,
            PerUserLimit = 1,
            IsEnabled = true,
        });
        dbContext.UserCoupons.Add(new UserCoupon
        {
            AppUserId = 2,
            CouponTemplateId = 20,
            CouponCode = "EXIST001",
            Status = UserCouponStatus.Unused,
            ReceivedAt = DateTime.Now,
            EffectiveAt = DateTime.Now,
            ExpireAt = DateTime.Now.AddDays(5),
        });
        await dbContext.SaveChangesAsync();

        var service = new UserCouponGrantService(dbContext);

        var result = await service.GrantAsync(20, [new() { AppUserId = 2, QuantityPerUser = 1 }]);

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Items);
        Assert.False(result.Items.First().Success);
        Assert.Contains("超过每人限领", result.Items.First().Message);
        Assert.Equal(1, await dbContext.UserCoupons.CountAsync(x => x.AppUserId == 2 && x.CouponTemplateId == 20));
    }

    [Fact]
    public async Task GrantAsync_Should_Fail_When_Template_Disabled()
    {
        await using var dbContext = CreateDbContext();
        dbContext.AppUsers.Add(new AppUser { Id = 3, MiniOpenId = "mini-3", OfficialOpenId = "gh-3" });
        dbContext.CouponTemplates.Add(new CouponTemplate
        {
            Id = 30,
            Name = "停用券",
            TemplateType = CouponTemplateType.FullReduction,
            ValidPeriodType = CouponValidPeriodType.FixedDateRange,
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today.AddDays(10),
            PerUserLimit = 2,
            IsEnabled = false,
        });
        await dbContext.SaveChangesAsync();

        var service = new UserCouponGrantService(dbContext);

        var result = await service.GrantAsync(30, [new() { AppUserId = 3, QuantityPerUser = 1 }]);

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Items);
        Assert.False(result.Items.First().Success);
        Assert.Contains("已停用", result.Items.First().Message);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}

using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Tests;

public class WeChatPaySettingsProviderTests
{
    [Fact]
    public async Task GetAsync_Should_Return_Seed_Row_On_First_Read()
    {
        await using var dbContext = CreateDbContext();
        SeedDefaultRow(dbContext);
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var provider = new WeChatPaySettingsProvider(dbContext);

        var snapshot = await provider.GetAsync();

        Assert.Equal(string.Empty, snapshot.AppId);
        Assert.True(snapshot.EnableMockFallback);
        Assert.False(snapshot.IsConfigured);
    }

    [Fact]
    public async Task SaveAsync_Should_Overwrite_NonNull_Fields_And_Keep_Null_Fields()
    {
        await using var dbContext = CreateDbContext();
        SeedDefaultRow(dbContext);
        dbContext.WeChatPaySettings.Single().ApiV3Key = "ORIG_V3";
        dbContext.WeChatPaySettings.Single().PrivateKeyPem = "ORIG_PEM";
        await dbContext.SaveChangesAsync();
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var provider = new WeChatPaySettingsProvider(dbContext);

        await provider.SaveAsync(new WeChatPaySettingsUpdate
        {
            AppId = "wxNEW",
            ApiV3Key = null,
            PrivateKeyPem = "",
        });

        var snapshot = await provider.GetAsync();
        Assert.Equal("wxNEW", snapshot.AppId);
        Assert.Equal("ORIG_V3", snapshot.ApiV3Key);
        Assert.Equal("ORIG_PEM", snapshot.PrivateKeyPem);
    }

    [Fact]
    public async Task SaveAsync_Should_Ignore_Display_Placeholder_For_Sensitive_Fields()
    {
        await using var dbContext = CreateDbContext();
        SeedDefaultRow(dbContext);
        dbContext.WeChatPaySettings.Single().PrivateKeyPem = "ORIG_PEM";
        await dbContext.SaveChangesAsync();
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var provider = new WeChatPaySettingsProvider(dbContext);

        await provider.SaveAsync(new WeChatPaySettingsUpdate { PrivateKeyPem = "已配置" });

        var snapshot = await provider.GetAsync();
        Assert.Equal("ORIG_PEM", snapshot.PrivateKeyPem);
    }

    [Fact]
    public async Task SaveAsync_Should_Invalidate_Cache_Immediately()
    {
        await using var dbContext = CreateDbContext();
        SeedDefaultRow(dbContext);
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var provider = new WeChatPaySettingsProvider(dbContext);

        _ = await provider.GetAsync();
        await provider.SaveAsync(new WeChatPaySettingsUpdate { AppId = "wxAFTER" });

        var snapshot = await provider.GetAsync();
        Assert.Equal("wxAFTER", snapshot.AppId);
    }

    [Fact]
    public async Task SaveAsync_Should_Update_UpdatedAt_Timestamp()
    {
        await using var dbContext = CreateDbContext();
        SeedDefaultRow(dbContext);
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var provider = new WeChatPaySettingsProvider(dbContext);
        var before = DateTime.UtcNow.AddSeconds(-1);

        await provider.SaveAsync(new WeChatPaySettingsUpdate { AppId = "wxT" });

        var snapshot = await provider.GetAsync();
        Assert.True(snapshot.UpdatedAt >= before);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedDefaultRow(AppDbContext dbContext)
    {
        dbContext.WeChatPaySettings.Add(new WeChatPaySetting
        {
            Id = 1,
            EnableMockFallback = true,
            UpdatedAt = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
        });
        dbContext.SaveChanges();
    }
}

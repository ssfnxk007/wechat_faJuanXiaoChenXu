using FaJuan.Api.Contracts;
using FaJuan.Api.Controllers;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Tests;

public class AdminMiniAppPayControllerTests
{
    [Fact]
    public async Task Get_Should_Mask_Sensitive_Fields_When_Configured()
    {
        await using var dbContext = CreateDbContext();
        SeedRow(dbContext, privateKey: "PEM_SECRET", apiV3: "V3_SECRET");
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var controller = new AdminMiniAppPayController(new WeChatPaySettingsProvider(dbContext));

        var response = ExtractOk(await controller.Get(default));

        Assert.Equal("已配置", response.PrivateKeyDisplay);
        Assert.Equal("已配置", response.ApiV3KeyDisplay);
        Assert.Equal("wxDEMO", response.AppId);
    }

    [Fact]
    public async Task Get_Should_Return_Empty_Display_When_Not_Configured()
    {
        await using var dbContext = CreateDbContext();
        SeedRow(dbContext, privateKey: "", apiV3: "");
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var controller = new AdminMiniAppPayController(new WeChatPaySettingsProvider(dbContext));

        var response = ExtractOk(await controller.Get(default));

        Assert.Equal(string.Empty, response.PrivateKeyDisplay);
        Assert.False(response.IsConfigured);
    }

    [Fact]
    public async Task Update_Should_Keep_Sensitive_Fields_When_Request_Sends_Empty()
    {
        await using var dbContext = CreateDbContext();
        SeedRow(dbContext, privateKey: "ORIG_PEM", apiV3: "ORIG_V3");
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var controller = new AdminMiniAppPayController(new WeChatPaySettingsProvider(dbContext));

        await controller.Update(new SaveAdminWeChatPaySettingsRequest
        {
            AppId = "wxNEW",
            PrivateKeyPem = "",
            ApiV3Key = null,
        }, default);

        var row = dbContext.WeChatPaySettings.AsNoTracking().Single();
        Assert.Equal("wxNEW", row.AppId);
        Assert.Equal("ORIG_PEM", row.PrivateKeyPem);
        Assert.Equal("ORIG_V3", row.ApiV3Key);
    }

    [Fact]
    public async Task Update_Should_Reject_Non_Https_Notify_Url()
    {
        await using var dbContext = CreateDbContext();
        SeedRow(dbContext);
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var controller = new AdminMiniAppPayController(new WeChatPaySettingsProvider(dbContext));

        var actionResult = await controller.Update(new SaveAdminWeChatPaySettingsRequest
        {
            NotifyUrl = "http://insecure.example.com/callback",
        }, default);

        var bad = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var body = Assert.IsType<ApiResponse<AdminWeChatPaySettingsDto>>(bad.Value);
        Assert.False(body.Success);
        Assert.Contains("https", body.Message);
    }

    [Fact]
    public async Task Update_Should_Reject_Malformed_Private_Key_Pem()
    {
        await using var dbContext = CreateDbContext();
        SeedRow(dbContext);
        WeChatPaySettingsProvider.InvalidateCacheForTests();
        var controller = new AdminMiniAppPayController(new WeChatPaySettingsProvider(dbContext));

        var actionResult = await controller.Update(new SaveAdminWeChatPaySettingsRequest
        {
            PrivateKeyPem = "not-a-pem-content",
        }, default);

        var bad = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var body = Assert.IsType<ApiResponse<AdminWeChatPaySettingsDto>>(bad.Value);
        Assert.False(body.Success);
        Assert.Contains("PEM", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedRow(AppDbContext dbContext, string privateKey = "", string apiV3 = "")
    {
        dbContext.WeChatPaySettings.Add(new WeChatPaySetting
        {
            Id = 1,
            AppId = "wxDEMO",
            MerchantId = "1600000000",
            MerchantSerialNo = "ABC123",
            PrivateKeyPem = privateKey,
            ApiV3Key = apiV3,
            NotifyUrl = "https://example.com/callback",
            EnableMockFallback = true,
            UpdatedAt = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
        });
        dbContext.SaveChanges();
    }

    private static AdminWeChatPaySettingsDto ExtractOk(ActionResult<ApiResponse<AdminWeChatPaySettingsDto>> actionResult)
    {
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var body = Assert.IsType<ApiResponse<AdminWeChatPaySettingsDto>>(ok.Value);
        Assert.True(body.Success);
        return body.Data!;
    }
}

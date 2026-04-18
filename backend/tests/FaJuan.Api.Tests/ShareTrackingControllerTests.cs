using System.Security.Claims;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Controllers;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Tests;

public class ShareTrackingControllerTests
{
    [Fact]
    public async Task SaveEvent_Should_Deduplicate_By_EventKey()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateMiniAppController(dbContext, userId: 101);

        var request = new SaveMiniAppShareTrackingEventRequest
        {
            EventType = ShareTrackingConstants.EventTypeShareIntent,
            EventKey = "shareIntent:abc",
            ShareId = "abc",
            TargetType = ShareTrackingConstants.TargetTypeActivity,
            TargetKey = "newcomer",
            PagePath = "/pages/activity/detail",
            Scene = "1044",
        };

        var firstResult = await controller.SaveEvent(request, CancellationToken.None);
        var firstPayload = UnwrapOkPayload<SaveMiniAppShareTrackingEventResult>(firstResult.Result);
        Assert.True(firstPayload.Data?.Accepted);
        Assert.False(firstPayload.Data?.Deduplicated);

        var secondResult = await controller.SaveEvent(request, CancellationToken.None);
        var secondPayload = UnwrapOkPayload<SaveMiniAppShareTrackingEventResult>(secondResult.Result);
        Assert.True(secondPayload.Data?.Accepted);
        Assert.True(secondPayload.Data?.Deduplicated);

        var totalEvents = await dbContext.MiniAppShareEvents.CountAsync();
        Assert.Equal(1, totalEvents);
    }

    [Fact]
    public async Task GetSummary_Should_Aggregate_ShareIntent_And_Open()
    {
        await using var dbContext = CreateDbContext();
        dbContext.MiniAppShareEvents.AddRange(
            new MiniAppShareEvent
            {
                EventType = ShareTrackingConstants.EventTypeShareIntent,
                EventKey = "shareIntent:1",
                ShareId = "sid-1",
                TargetType = ShareTrackingConstants.TargetTypeCoupon,
                TargetKey = "template:12",
                TargetId = 12,
                PagePath = "/pages/coupon/detail",
                CreatedAt = DateTime.Today,
            },
            new MiniAppShareEvent
            {
                EventType = ShareTrackingConstants.EventTypeShareIntent,
                EventKey = "shareIntent:2",
                ShareId = "sid-2",
                TargetType = ShareTrackingConstants.TargetTypeCoupon,
                TargetKey = "template:12",
                TargetId = 12,
                PagePath = "/pages/coupon/detail",
                CreatedAt = DateTime.Today,
            },
            new MiniAppShareEvent
            {
                EventType = ShareTrackingConstants.EventTypeOpen,
                EventKey = "open:1:u:201:/pages/coupon/detail",
                ShareId = "sid-1",
                OpenUserId = 201,
                TargetType = ShareTrackingConstants.TargetTypeCoupon,
                TargetKey = "template:12",
                TargetId = 12,
                PagePath = "/pages/coupon/detail",
                CreatedAt = DateTime.Today,
            });
        await dbContext.SaveChangesAsync();

        var controller = new ShareTrackingController(dbContext);
        var result = await controller.GetSummary(null, null, "coupon", "template:12", null, CancellationToken.None);
        var payload = UnwrapOkPayload<IReadOnlyCollection<ShareTrackingSummaryItemDto>>(result.Result);
        var item = Assert.Single(payload.Data!);
        Assert.Equal(2, item.ShareIntentCount);
        Assert.Equal(1, item.OpenCount);
        Assert.Equal(0.5m, item.OpenRate);
    }

    private static ApiResponse<T> UnwrapOkPayload<T>(IActionResult? actionResult)
    {
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        return Assert.IsType<ApiResponse<T>>(okResult.Value);
    }

    private static MiniAppShareTrackingController CreateMiniAppController(AppDbContext dbContext, long? userId)
    {
        var controller = new MiniAppShareTrackingController(dbContext);
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim("userId", userId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
        context.Request.Headers.UserAgent = "unit-test-agent";
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        return controller;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}

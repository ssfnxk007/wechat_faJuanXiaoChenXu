using System.Text.Json;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Route("api/miniapp/share-tracking")]
public class MiniAppShareTrackingController(AppDbContext dbContext) : ApiControllerBase
{
    [MiniAppAuthorize(Optional = true)]
    [HttpPost("events")]
    public async Task<ActionResult<ApiResponse<SaveMiniAppShareTrackingEventResult>>> SaveEvent(
        [FromBody] SaveMiniAppShareTrackingEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventType = request.EventType?.Trim();
        if (eventType != ShareTrackingConstants.EventTypeShareIntent && eventType != ShareTrackingConstants.EventTypeOpen)
        {
            return BadRequest(Failure<SaveMiniAppShareTrackingEventResult>("eventType 仅支持 shareIntent/open"));
        }

        var targetType = request.TargetType?.Trim();
        if (targetType != ShareTrackingConstants.TargetTypeActivity && targetType != ShareTrackingConstants.TargetTypeCoupon)
        {
            return BadRequest(Failure<SaveMiniAppShareTrackingEventResult>("targetType 仅支持 activity/coupon"));
        }

        var eventKey = request.EventKey?.Trim();
        if (string.IsNullOrWhiteSpace(eventKey))
        {
            return BadRequest(Failure<SaveMiniAppShareTrackingEventResult>("eventKey 不能为空"));
        }

        var shareId = request.ShareId?.Trim();
        if (string.IsNullOrWhiteSpace(shareId))
        {
            return BadRequest(Failure<SaveMiniAppShareTrackingEventResult>("shareId 不能为空"));
        }

        var targetKey = request.TargetKey?.Trim();
        if (string.IsNullOrWhiteSpace(targetKey))
        {
            return BadRequest(Failure<SaveMiniAppShareTrackingEventResult>("targetKey 不能为空"));
        }

        var pagePath = request.PagePath?.Trim();
        if (string.IsNullOrWhiteSpace(pagePath))
        {
            return BadRequest(Failure<SaveMiniAppShareTrackingEventResult>("pagePath 不能为空"));
        }

        var deduplicated = await dbContext.MiniAppShareEvents.AsNoTracking()
            .AnyAsync(x => x.EventKey == eventKey, cancellationToken);
        if (deduplicated)
        {
            return Ok(Success(new SaveMiniAppShareTrackingEventResult
            {
                Accepted = true,
                Deduplicated = true,
            }));
        }

        var currentUserId = GetCurrentUserId();
        var queryJson = request.Query is null || request.Query.Count == 0
            ? null
            : JsonSerializer.Serialize(request.Query);

        var entity = new MiniAppShareEvent
        {
            EventType = eventType,
            EventKey = eventKey,
            ShareId = shareId,
            FromUserId = eventType == ShareTrackingConstants.EventTypeShareIntent ? currentUserId : null,
            OpenUserId = eventType == ShareTrackingConstants.EventTypeOpen ? currentUserId : null,
            VisitorKey = string.IsNullOrWhiteSpace(request.VisitorKey) ? null : request.VisitorKey.Trim(),
            TargetType = targetType,
            TargetKey = targetKey,
            TargetId = request.TargetId,
            PagePath = pagePath,
            Scene = string.IsNullOrWhiteSpace(request.Scene) ? null : request.Scene.Trim(),
            QueryJson = queryJson,
            ClientTime = request.ClientTime,
            Ip = GetRequestIpAddress(),
            UserAgent = GetRequestUserAgent(),
            CreatedAt = DateTime.Now,
        };

        dbContext.MiniAppShareEvents.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Ok(Success(new SaveMiniAppShareTrackingEventResult
            {
                Accepted = true,
                Deduplicated = true,
            }));
        }

        return Ok(Success(new SaveMiniAppShareTrackingEventResult
        {
            Accepted = true,
            Deduplicated = false,
        }));
    }

    private string? GetRequestIpAddress()
    {
        var value = HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(value))
        {
            var candidate = value.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Length > 64 ? candidate[..64] : candidate;
            }
        }

        value = HttpContext?.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length > 64 ? value[..64] : value;
    }

    private string? GetRequestUserAgent()
    {
        var value = HttpContext?.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length > 256 ? value[..256] : value;
    }
}

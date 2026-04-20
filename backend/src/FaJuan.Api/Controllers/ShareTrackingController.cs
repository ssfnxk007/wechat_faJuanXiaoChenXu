using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/share-tracking")]
[Route("api/share-tracking")]
public class ShareTrackingController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ShareTrackingSummaryItemDto>>>> GetSummary(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? targetType,
        [FromQuery] string? targetKey,
        [FromQuery] long? couponTemplateId,
        CancellationToken cancellationToken)
    {
        var range = NormalizeDateRange(dateFrom, dateTo);
        var query = BuildQuery(range.From, range.ToInclusive, targetType, targetKey, couponTemplateId);

        var groupedItems = await query
            .GroupBy(x => new
            {
                Date = x.CreatedAt.Date,
                x.TargetType,
                x.TargetKey,
                x.TargetId,
            })
            .Select(g => new
            {
                g.Key.Date,
                g.Key.TargetType,
                g.Key.TargetKey,
                g.Key.TargetId,
                ShareIntentCount = g.Count(x => x.EventType == ShareTrackingConstants.EventTypeShareIntent),
                OpenCount = g.Count(x => x.EventType == ShareTrackingConstants.EventTypeOpen),
            })
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.TargetType)
            .ThenBy(x => x.TargetKey)
            .ToListAsync(cancellationToken);

        var items = groupedItems.Select(x =>
        {
            var openRate = x.ShareIntentCount > 0
                ? Math.Round((decimal)x.OpenCount / x.ShareIntentCount, 4, MidpointRounding.AwayFromZero)
                : 0m;

            return new ShareTrackingSummaryItemDto
            {
                Date = x.Date,
                TargetType = x.TargetType,
                TargetKey = x.TargetKey,
                TargetId = x.TargetId,
                ShareIntentCount = x.ShareIntentCount,
                OpenCount = x.OpenCount,
                OpenRate = openRate,
            };
        }).ToList();

        return Ok(Success<IReadOnlyCollection<ShareTrackingSummaryItemDto>>(items));
    }

    [HttpGet("details")]
    public async Task<ActionResult<ApiResponse<PagedResult<ShareTrackingDetailItemDto>>>> GetDetails(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? targetType,
        [FromQuery] string? targetKey,
        [FromQuery] long? couponTemplateId,
        [FromQuery] string? eventType,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageIndex = pageIndex <= 0 ? 1 : pageIndex;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 200);

        var range = NormalizeDateRange(dateFrom, dateTo);
        var query = BuildQuery(range.From, range.ToInclusive, targetType, targetKey, couponTemplateId);

        var normalizedEventType = eventType?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedEventType))
        {
            query = query.Where(x => x.EventType == normalizedEventType);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new ShareTrackingDetailItemDto
            {
                Id = x.Id,
                EventType = x.EventType,
                ShareId = x.ShareId,
                EventKey = x.EventKey,
                FromUserId = x.FromUserId,
                OpenUserId = x.OpenUserId,
                VisitorKey = x.VisitorKey,
                TargetType = x.TargetType,
                TargetKey = x.TargetKey,
                TargetId = x.TargetId,
                PagePath = x.PagePath,
                Scene = x.Scene,
                ClientTime = x.ClientTime,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(Success(new PagedResult<ShareTrackingDetailItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    private IQueryable<Domain.Entities.MiniAppShareEvent> BuildQuery(
        DateTime fromInclusive,
        DateTime toInclusive,
        string? targetType,
        string? targetKey,
        long? couponTemplateId)
    {
        var query = dbContext.MiniAppShareEvents.AsNoTracking()
            .Where(x => x.CreatedAt >= fromInclusive && x.CreatedAt <= toInclusive);

        var normalizedTargetType = targetType?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedTargetType))
        {
            query = query.Where(x => x.TargetType == normalizedTargetType);
        }

        var normalizedTargetKey = targetKey?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedTargetKey))
        {
            query = query.Where(x => x.TargetKey == normalizedTargetKey);
        }

        if (couponTemplateId.HasValue && couponTemplateId.Value > 0)
        {
            var couponTargetKey = $"template:{couponTemplateId.Value}";
            query = query.Where(x => x.TargetType == ShareTrackingConstants.TargetTypeCoupon && x.TargetKey == couponTargetKey);
        }

        return query;
    }

    private static (DateTime From, DateTime ToInclusive) NormalizeDateRange(DateTime? dateFrom, DateTime? dateTo)
    {
        var today = DateTime.Today;
        var toDate = (dateTo ?? today).Date;
        var fromDate = (dateFrom ?? toDate.AddDays(-6)).Date;

        if (fromDate > toDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        return (fromDate, toDate.AddDays(1).AddTicks(-1));
    }
}

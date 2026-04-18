namespace FaJuan.Api.Contracts;

public static class ShareTrackingConstants
{
    public const string EventTypeShareIntent = "shareIntent";
    public const string EventTypeOpen = "open";

    public const string TargetTypeActivity = "activity";
    public const string TargetTypeCoupon = "coupon";
}

public class SaveMiniAppShareTrackingEventRequest
{
    public string EventType { get; init; } = string.Empty;
    public string EventKey { get; init; } = string.Empty;
    public string ShareId { get; init; } = string.Empty;
    public string TargetType { get; init; } = string.Empty;
    public string TargetKey { get; init; } = string.Empty;
    public long? TargetId { get; init; }
    public string PagePath { get; init; } = string.Empty;
    public string? VisitorKey { get; init; }
    public string? Scene { get; init; }
    public IDictionary<string, string>? Query { get; init; }
    public DateTime? ClientTime { get; init; }
}

public class SaveMiniAppShareTrackingEventResult
{
    public bool Accepted { get; init; }
    public bool Deduplicated { get; init; }
}

public class ShareTrackingSummaryItemDto
{
    public DateTime Date { get; init; }
    public string TargetType { get; init; } = string.Empty;
    public string TargetKey { get; init; } = string.Empty;
    public long? TargetId { get; init; }
    public int ShareIntentCount { get; init; }
    public int OpenCount { get; init; }
    public decimal OpenRate { get; init; }
}

public class ShareTrackingDetailItemDto
{
    public long Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string ShareId { get; init; } = string.Empty;
    public string EventKey { get; init; } = string.Empty;
    public long? FromUserId { get; init; }
    public long? OpenUserId { get; init; }
    public string? VisitorKey { get; init; }
    public string TargetType { get; init; } = string.Empty;
    public string TargetKey { get; init; } = string.Empty;
    public long? TargetId { get; init; }
    public string PagePath { get; init; } = string.Empty;
    public string? Scene { get; init; }
    public DateTime? ClientTime { get; init; }
    public DateTime CreatedAt { get; init; }
}

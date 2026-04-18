namespace FaJuan.Api.Domain.Entities;

public class MiniAppShareEvent
{
    public long Id { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ShareId { get; set; } = string.Empty;
    public long? FromUserId { get; set; }
    public long? OpenUserId { get; set; }
    public string? VisitorKey { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string TargetKey { get; set; } = string.Empty;
    public long? TargetId { get; set; }
    public string PagePath { get; set; } = string.Empty;
    public string? Scene { get; set; }
    public string? QueryJson { get; set; }
    public DateTime? ClientTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
}

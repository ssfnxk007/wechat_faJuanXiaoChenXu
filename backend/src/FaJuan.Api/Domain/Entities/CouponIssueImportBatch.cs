namespace FaJuan.Api.Domain.Entities;

public class CouponIssueImportBatch
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? FileName { get; set; }
    public int TotalCount { get; set; }
    public int MatchedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

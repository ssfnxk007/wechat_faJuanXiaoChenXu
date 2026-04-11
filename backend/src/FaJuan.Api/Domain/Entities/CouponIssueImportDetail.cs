using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Domain.Entities;

public class CouponIssueImportDetail
{
    public long Id { get; set; }
    public long BatchId { get; set; }
    public string? Mobile { get; set; }
    public string? MiniOpenId { get; set; }
    public string? OfficialOpenId { get; set; }
    public long CouponTemplateId { get; set; }
    public int Quantity { get; set; }
    public CouponIssueImportDetailStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

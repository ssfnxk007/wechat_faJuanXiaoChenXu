using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Domain.Entities;

public class CouponTemplate
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? ImageAssetId { get; set; }
    public CouponTemplateType TemplateType { get; set; }
    public CouponValidPeriodType ValidPeriodType { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? ThresholdAmount { get; set; }
    public int? ValidDays { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsNewUserOnly { get; set; }
    public bool IsAllStores { get; set; } = true;
    public int PerUserLimit { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

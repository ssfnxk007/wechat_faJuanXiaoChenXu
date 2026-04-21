using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Contracts;

public class CouponTemplateListItemDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public long? ImageAssetId { get; init; }
    public string? ImageUrl { get; init; }
    public CouponTemplateType TemplateType { get; init; }
    public CouponValidPeriodType ValidPeriodType { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public int? ValidDays { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool IsNewUserOnly { get; init; }
    public bool IsAllStores { get; init; }
    public int PerUserLimit { get; init; }
    public bool IsEnabled { get; init; }
    public CouponDistributionMode DistributionMode { get; init; }
    public decimal? SalePrice { get; init; }
    public string? Remark { get; init; }
    public IReadOnlyCollection<long> ProductIds { get; init; } = [];
    public IReadOnlyCollection<long> StoreIds { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public class SaveCouponTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public long? ImageAssetId { get; init; }
    public CouponTemplateType TemplateType { get; init; }
    public CouponValidPeriodType ValidPeriodType { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public int? ValidDays { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool IsNewUserOnly { get; init; }
    public bool IsAllStores { get; init; } = true;
    public int PerUserLimit { get; init; } = 1;
    public bool IsEnabled { get; init; } = true;
    public CouponDistributionMode DistributionMode { get; init; } = CouponDistributionMode.FreeClaim;
    public decimal? SalePrice { get; init; }
    public string? Remark { get; init; }
    public IReadOnlyCollection<long> ProductIds { get; init; } = [];
    public IReadOnlyCollection<long> StoreIds { get; init; } = [];
}

using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Contracts;

public class ProductListItemDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ErpProductCode { get; init; } = string.Empty;
    public string? ErpIsbnCode { get; init; }
    public long? MainImageAssetId { get; init; }
    public string? MainImageUrl { get; init; }
    public IReadOnlyCollection<long> DetailImageAssetIds { get; init; } = [];
    public IReadOnlyCollection<string> DetailImageUrls { get; init; } = [];
    public decimal? ErpOriginalPrice { get; init; }
    public decimal? SalePrice { get; init; }
    public int? StockQuantity { get; init; }
    public bool? ShowInMiniApp { get; init; }
    public CouponValidPeriodType? DirectPurchaseValidPeriodType { get; init; }
    public int? DirectPurchaseValidDays { get; init; }
    public DateTime? DirectPurchaseValidFrom { get; init; }
    public DateTime? DirectPurchaseValidTo { get; init; }
    public long? DirectPurchaseCouponTemplateId { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class SaveProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string ErpProductCode { get; init; } = string.Empty;
    public string? ErpIsbnCode { get; init; }
    public long? MainImageAssetId { get; init; }
    public IReadOnlyCollection<long> DetailImageAssetIds { get; init; } = [];
    public decimal? ErpOriginalPrice { get; init; }
    public decimal? SalePrice { get; init; }
    public int? StockQuantity { get; init; }
    public bool ShowInMiniApp { get; init; }
    public CouponValidPeriodType? DirectPurchaseValidPeriodType { get; init; }
    public int? DirectPurchaseValidDays { get; init; }
    public DateTime? DirectPurchaseValidFrom { get; init; }
    public DateTime? DirectPurchaseValidTo { get; init; }
    public bool IsEnabled { get; init; } = true;
}

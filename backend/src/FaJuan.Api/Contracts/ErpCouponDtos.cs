namespace FaJuan.Api.Contracts;

public class ErpCouponPreviewRequest
{
    public string SiteCode { get; init; } = string.Empty;
    public string CouponCode { get; init; } = string.Empty;
}

public class ErpCouponWriteOffRequest
{
    public string SiteCode { get; init; } = string.Empty;
    public string CouponCode { get; init; } = string.Empty;
    public string? SelectedProductCode { get; init; }
    public string? OperatorName { get; init; }
    public string? DeviceCode { get; init; }
}

public class ErpCouponPreviewDto
{
    public string SiteCode { get; init; } = string.Empty;
    public long? StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string CouponCode { get; init; } = string.Empty;
    public long? UserCouponId { get; init; }
    public long? AppUserId { get; init; }
    public long? CouponTemplateId { get; init; }
    public string CouponTemplateName { get; init; } = string.Empty;
    public int? TemplateType { get; init; }
    public int? Status { get; init; }
    public string SettlementType { get; init; } = string.Empty;
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public DateTime? EffectiveAt { get; init; }
    public DateTime? ExpireAt { get; init; }
    public bool CanWriteOff { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyCollection<ErpCouponProductOptionDto> ProductScope { get; init; } = [];
}

public class ErpCouponProductOptionDto
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ErpProductCode { get; init; } = string.Empty;
}

public class ErpCouponWriteOffResultDto
{
    public long UserCouponId { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public long AppUserId { get; init; }
    public long CouponTemplateId { get; init; }
    public string CouponTemplateName { get; init; } = string.Empty;
    public string SiteCode { get; init; } = string.Empty;
    public long StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string SettlementType { get; init; } = string.Empty;
    public string? SelectedProductCode { get; init; }
    public string? SelectedProductName { get; init; }
    public string Message { get; init; } = string.Empty;
}

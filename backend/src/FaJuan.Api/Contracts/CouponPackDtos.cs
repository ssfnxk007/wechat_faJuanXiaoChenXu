namespace FaJuan.Api.Contracts;

public class CouponPackListItemDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public long? ImageAssetId { get; init; }
    public string? ImageUrl { get; init; }
    public decimal SalePrice { get; init; }
    public int Status { get; init; }
    public int PerUserLimit { get; init; }
    public DateTime? SaleStartTime { get; init; }
    public DateTime? SaleEndTime { get; init; }
    public string? Remark { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class SaveCouponPackRequest
{
    public string Name { get; init; } = string.Empty;
    public long? ImageAssetId { get; init; }
    public decimal SalePrice { get; init; }
    public int Status { get; init; } = 1;
    public int PerUserLimit { get; init; } = 1;
    public DateTime? SaleStartTime { get; init; }
    public DateTime? SaleEndTime { get; init; }
    public string? Remark { get; init; }
}

public class CreateCouponOrderRequest
{
    public long UserId { get; init; }
    public long CouponPackId { get; init; }
}

public class CouponOrderListItemDto
{
    public long Id { get; init; }
    public string OrderNo { get; init; } = string.Empty;
    public long AppUserId { get; init; }
    public long? CouponPackId { get; init; }
    public long? CouponTemplateId { get; init; }
    public decimal OrderAmount { get; init; }
    public int Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CouponOrderDetailDto
{
    public long Id { get; init; }
    public string OrderNo { get; init; } = string.Empty;
    public long AppUserId { get; init; }
    public long? CouponPackId { get; init; }
    public string? CouponPackName { get; init; }
    public long? CouponTemplateId { get; init; }
    public string? CouponTemplateName { get; init; }
    public decimal OrderAmount { get; init; }
    public int Status { get; init; }
    public string? PaymentNo { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyCollection<CouponOrderPaymentDto> Payments { get; init; } = Array.Empty<CouponOrderPaymentDto>();
    public IReadOnlyCollection<CouponOrderGrantedCouponDto> GrantedCoupons { get; init; } = Array.Empty<CouponOrderGrantedCouponDto>();
}

public class CouponOrderPaymentDto
{
    public long Id { get; init; }
    public string PaymentNo { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int Status { get; init; }
    public string? ChannelTradeNo { get; init; }
    public string? RawCallback { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CouponOrderGrantedCouponDto
{
    public long Id { get; init; }
    public long CouponTemplateId { get; init; }
    public string CouponTemplateName { get; init; } = string.Empty;
    public int TemplateType { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public bool IsAllStores { get; init; }
    public bool IsNewUserOnly { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public int Status { get; init; }
    public DateTime ReceivedAt { get; init; }
    public DateTime EffectiveAt { get; init; }
    public DateTime ExpireAt { get; init; }
}

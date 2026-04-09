namespace FaJuan.Api.Contracts;

public class CouponPackItemDto
{
    public long Id { get; init; }
    public long CouponPackId { get; init; }
    public long CouponTemplateId { get; init; }
    public int Quantity { get; init; }
    public string CouponTemplateName { get; init; } = string.Empty;
}

public class SaveCouponPackItemRequest
{
    public long CouponPackId { get; init; }
    public long CouponTemplateId { get; init; }
    public int Quantity { get; init; }
}

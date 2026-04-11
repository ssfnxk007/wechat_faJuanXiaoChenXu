namespace FaJuan.Api.Contracts;

public class MiniAppHomeDto
{
    public MiniAppThemeDto Theme { get; init; } = new();
    public IReadOnlyCollection<MiniAppBannerDto> Banners { get; init; } = [];
    public IReadOnlyCollection<MiniAppCouponPackCardDto> FeaturedCouponPacks { get; init; } = [];
    public IReadOnlyCollection<MiniAppProductCardDto> RecommendedProducts { get; init; } = [];
    public IReadOnlyCollection<MiniAppCouponTemplateCardDto> DirectCoupons { get; init; } = [];
    public MiniAppUserSummaryDto? UserSummary { get; init; }
}

public class MiniAppThemeDto
{
    public string ThemeCode { get; init; } = "green";
}

public class AdminMiniAppThemeSettingsDto
{
    public string ThemeCode { get; init; } = "green";
}

public class SaveAdminMiniAppThemeSettingsRequest
{
    public string ThemeCode { get; init; } = "green";
}

public class MiniAppBannerDto
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string? LinkUrl { get; init; }
    public int Sort { get; init; }
}

public class MiniAppCouponPackCardDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal SalePrice { get; init; }
    public int PerUserLimit { get; init; }
    public string? Remark { get; init; }
    public DateTime? SaleStartTime { get; init; }
    public DateTime? SaleEndTime { get; init; }
}

public class MiniAppProductCardDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ErpProductCode { get; init; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public decimal? SalePrice { get; init; }
}

public class MiniAppCouponTemplateCardDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TemplateType { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public bool IsNewUserOnly { get; init; }
    public bool IsAllStores { get; init; }
    public int ValidPeriodType { get; init; }
    public int? ValidDays { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public string? Remark { get; init; }
}

public class MiniAppUserSummaryDto
{
    public long UserId { get; init; }
    public string? Nickname { get; init; }
    public bool IsNewUser { get; init; }
    public int UnusedCouponCount { get; init; }
}

public class MiniAppCouponPackDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal SalePrice { get; init; }
    public int PerUserLimit { get; init; }
    public string? Remark { get; init; }
    public DateTime? SaleStartTime { get; init; }
    public DateTime? SaleEndTime { get; init; }
    public IReadOnlyCollection<MiniAppCouponPackItemDto> Items { get; init; } = [];
}

public class MiniAppCouponPackItemDto
{
    public long CouponTemplateId { get; init; }
    public string CouponTemplateName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int TemplateType { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public bool IsNewUserOnly { get; init; }
    public bool IsAllStores { get; init; }
}

public class MiniAppUserCouponCardDto
{
    public long Id { get; init; }
    public long CouponTemplateId { get; init; }
    public string CouponTemplateName { get; init; } = string.Empty;
    public int TemplateType { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public int Status { get; init; }
    public DateTime EffectiveAt { get; init; }
    public DateTime ExpireAt { get; init; }
    public DateTime ReceivedAt { get; init; }
    public bool IsAllStores { get; init; }
    public bool IsNewUserOnly { get; init; }
    public string? ImageUrl { get; set; }
}

public class MiniAppCouponDetailDto
{
    public long Id { get; init; }
    public long AppUserId { get; init; }
    public long CouponTemplateId { get; init; }
    public string CouponTemplateName { get; init; } = string.Empty;
    public string CouponCode { get; init; } = string.Empty;
    public string? QrPayload { get; init; }
    public int TemplateType { get; init; }
    public int ValidPeriodType { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public int? ValidDays { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool IsNewUserOnly { get; init; }
    public bool IsAllStores { get; init; }
    public int PerUserLimit { get; init; }
    public string? TemplateRemark { get; init; }
    public int Status { get; init; }
    public DateTime EffectiveAt { get; init; }
    public DateTime ExpireAt { get; init; }
    public DateTime ReceivedAt { get; init; }
    public string? ImageUrl { get; set; }
    public IReadOnlyCollection<MiniAppWriteOffRecordDto> WriteOffRecords { get; init; } = [];
}

public class MiniAppCouponTemplateDetailDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TemplateType { get; init; }
    public int ValidPeriodType { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? ThresholdAmount { get; init; }
    public int? ValidDays { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool IsNewUserOnly { get; init; }
    public bool IsAllStores { get; init; }
    public int PerUserLimit { get; init; }
    public string? TemplateRemark { get; init; }
    public bool CanClaim { get; init; }
    public int ClaimedCount { get; init; }
}

public class MiniAppClaimCouponResultDto
{
    public long UserCouponId { get; init; }
    public long CouponTemplateId { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public DateTime EffectiveAt { get; init; }
    public DateTime ExpireAt { get; init; }
}

public class MiniAppWriteOffRecordDto
{
    public long Id { get; init; }
    public long StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string? OperatorName { get; init; }
    public string? DeviceCode { get; init; }
    public DateTime WriteOffAt { get; init; }
}

public class MiniAppCreateOrderRequest
{
    public long UserId { get; init; }
    public long CouponPackId { get; init; }
}

public class MiniAppCreateOrderResultDto
{
    public long OrderId { get; init; }
    public string OrderNo { get; init; } = string.Empty;
    public long CouponPackId { get; init; }
    public string CouponPackName { get; init; } = string.Empty;
    public decimal OrderAmount { get; init; }
    public int Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class MiniAppOrderCardDto
{
    public long Id { get; init; }
    public string OrderNo { get; init; } = string.Empty;
    public long CouponPackId { get; init; }
    public string CouponPackName { get; init; } = string.Empty;
    public decimal OrderAmount { get; init; }
    public int Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? PaymentNo { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class MiniAppOrderDetailDto
{
    public long Id { get; init; }
    public string OrderNo { get; init; } = string.Empty;
    public long AppUserId { get; init; }
    public long CouponPackId { get; init; }
    public string CouponPackName { get; init; } = string.Empty;
    public decimal OrderAmount { get; init; }
    public int Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? PaymentNo { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyCollection<MiniAppUserCouponCardDto> GrantedCoupons { get; init; } = [];
}

public class MiniAppCreateOrderPaymentRequest
{
    public long UserId { get; init; }
}

public class MiniAppClaimCouponRequest
{
}

public class MiniAppCreateOrderPaymentResultDto
{
    public long OrderId { get; init; }
    public string OrderNo { get; init; } = string.Empty;
    public int OrderStatus { get; init; }
    public bool Paid { get; init; }
    public string Message { get; init; } = string.Empty;
    public CreatePaymentResultDto Payment { get; init; } = new();
}

public class MiniAppCompleteOrderPaymentRequest
{
    public long UserId { get; init; }
    public string? PaymentNo { get; init; }
    public string? ChannelTradeNo { get; init; }
    public string? RawCallback { get; init; }
}

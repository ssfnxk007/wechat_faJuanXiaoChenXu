namespace FaJuan.Api.Contracts;

public class UserCouponListItemDto
{
    public long Id { get; init; }
    public long AppUserId { get; init; }
    public long CouponTemplateId { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public int Status { get; init; }
    public DateTime EffectiveAt { get; init; }
    public DateTime ExpireAt { get; init; }
    public DateTime ReceivedAt { get; init; }
}

public class UserCouponDetailDto
{
    public long Id { get; init; }
    public long AppUserId { get; init; }
    public long CouponTemplateId { get; init; }
    public string CouponTemplateName { get; init; } = string.Empty;
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
    public bool TemplateEnabled { get; init; }
    public string? TemplateRemark { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public int Status { get; init; }
    public DateTime EffectiveAt { get; init; }
    public DateTime ExpireAt { get; init; }
    public DateTime ReceivedAt { get; init; }
}

public class CouponWriteOffRequest
{
    public string CouponCode { get; init; } = string.Empty;
    public long StoreId { get; init; }
    public long? ProductId { get; init; }
    public string? OperatorName { get; init; }
    public string? DeviceCode { get; init; }
}

public class CouponWriteOffResultDto
{
    public long UserCouponId { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public long AppUserId { get; init; }
    public long CouponTemplateId { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class CouponWriteOffRecordDto
{
    public long Id { get; init; }
    public long UserCouponId { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public long StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public long? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductCode { get; init; }
    public string? OperatorName { get; init; }
    public string? DeviceCode { get; init; }
    public DateTime WriteOffAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class ManualGrantUserCouponsRequest
{
    public long CouponTemplateId { get; init; }
    public long[] AppUserIds { get; init; } = [];
    public int QuantityPerUser { get; init; } = 1;
}

public class ManualGrantUserCouponInput
{
    public long AppUserId { get; init; }
    public int QuantityPerUser { get; init; } = 1;
}

public class ManualGrantUserCouponItemDto
{
    public long AppUserId { get; init; }
    public bool Success { get; init; }
    public int GrantedCount { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class ManualGrantUserCouponsResultDto
{
    public long CouponTemplateId { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public IReadOnlyCollection<ManualGrantUserCouponItemDto> Items { get; init; } = [];
}

public class ImportUserCouponsCsvResultDto
{
    public long CouponTemplateId { get; init; }
    public int ParsedRowCount { get; init; }
    public int AcceptedRowCount { get; init; }
    public int InvalidRowCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public IReadOnlyCollection<string> InvalidRows { get; init; } = [];
    public IReadOnlyCollection<ManualGrantUserCouponItemDto> Items { get; init; } = [];
}

public class ImportGrantUserCouponRowDto
{
    public long AppUserId { get; init; }
    public long CouponTemplateId { get; init; }
    public int QuantityPerUser { get; init; } = 1;
}

public class ImportGrantUserCouponsRequest
{
    public IFormFile? File { get; init; }
    public long CouponTemplateId { get; init; }
    public int? QuantityPerUser { get; init; }
    public string? CsvContent { get; init; }
}

public class ImportGrantUserCouponsResultDto
{
    public long CouponTemplateId { get; init; }
    public int QuantityPerUser { get; init; }
    public int TotalRows { get; init; }
    public int ParsedUserCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public IReadOnlyCollection<string> InvalidRows { get; init; } = [];
    public IReadOnlyCollection<ManualGrantUserCouponItemDto> Items { get; init; } = [];
}

using System.ComponentModel.DataAnnotations;
using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Domain.Entities;

public class UserCoupon
{
    public long Id { get; set; }
    public long AppUserId { get; set; }
    public long CouponTemplateId { get; set; }
    public long? CouponOrderId { get; set; }
    public CouponSourceType SourceType { get; set; } = CouponSourceType.CouponTemplate;
    public long? BoundProductId { get; set; }
    public string? BoundProductName { get; set; }
    public string? BoundErpProductCode { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public UserCouponStatus Status { get; set; } = UserCouponStatus.Unused;
    public CouponFulfillmentStatus FulfillmentStatus { get; set; } = CouponFulfillmentStatus.None;
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public DateTime EffectiveAt { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

using System.ComponentModel.DataAnnotations;
using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Domain.Entities;

public class UserCoupon
{
    public long Id { get; set; }
    public long AppUserId { get; set; }
    public long CouponTemplateId { get; set; }
    public long? CouponOrderId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public UserCouponStatus Status { get; set; } = UserCouponStatus.Unused;
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public DateTime EffectiveAt { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 乐观并发令牌：防止并发核销造成双写
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

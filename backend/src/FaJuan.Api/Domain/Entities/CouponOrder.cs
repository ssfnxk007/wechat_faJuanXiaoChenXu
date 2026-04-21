using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Domain.Entities;

public class CouponOrder
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long AppUserId { get; set; }
    public long? CouponPackId { get; set; }
    public long? CouponTemplateId { get; set; }
    public decimal OrderAmount { get; set; }
    public CouponOrderStatus Status { get; set; } = CouponOrderStatus.PendingPayment;
    public DateTime? PaidAt { get; set; }
    public string? PaymentNo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

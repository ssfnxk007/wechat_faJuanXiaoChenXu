using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Domain.Entities;

public class PaymentTransaction
{
    public long Id { get; set; }
    public long CouponOrderId { get; set; }
    public string PaymentNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ChannelTradeNo { get; set; }
    public string? RawCallback { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

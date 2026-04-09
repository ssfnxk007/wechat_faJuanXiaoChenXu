namespace FaJuan.Api.Domain.Entities;

public class CouponPackItem
{
    public long Id { get; set; }
    public long CouponPackId { get; set; }
    public long CouponTemplateId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

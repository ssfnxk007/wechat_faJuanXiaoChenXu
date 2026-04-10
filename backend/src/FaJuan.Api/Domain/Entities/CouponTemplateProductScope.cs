namespace FaJuan.Api.Domain.Entities;

public class CouponTemplateProductScope
{
    public long Id { get; set; }
    public long CouponTemplateId { get; set; }
    public long ProductId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

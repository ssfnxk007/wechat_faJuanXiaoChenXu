namespace FaJuan.Api.Domain.Entities;

public class CouponTemplateStoreScope
{
    public long Id { get; set; }
    public long CouponTemplateId { get; set; }
    public long StoreId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

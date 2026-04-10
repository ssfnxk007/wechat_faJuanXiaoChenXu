using FaJuan.Api.Domain.Enums;

namespace FaJuan.Api.Domain.Entities;

public class CouponPack
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? ImageAssetId { get; set; }
    public decimal SalePrice { get; set; }
    public CouponPackStatus Status { get; set; } = CouponPackStatus.Enabled;
    public DateTime? SaleStartTime { get; set; }
    public DateTime? SaleEndTime { get; set; }
    public int PerUserLimit { get; set; } = 1;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

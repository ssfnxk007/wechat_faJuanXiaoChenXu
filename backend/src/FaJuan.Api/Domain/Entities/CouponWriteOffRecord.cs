namespace FaJuan.Api.Domain.Entities;

public class CouponWriteOffRecord
{
    public long Id { get; set; }
    public long UserCouponId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public long StoreId { get; set; }
    public long? ProductId { get; set; }
    public string? OperatorName { get; set; }
    public string? DeviceCode { get; set; }
    public DateTime WriteOffAt { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

namespace FaJuan.Api.Domain.Entities;

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ErpProductCode { get; set; } = string.Empty;
    public decimal? SalePrice { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

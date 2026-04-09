namespace FaJuan.Api.Contracts;

public class ProductListItemDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ErpProductCode { get; init; } = string.Empty;
    public decimal? SalePrice { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class SaveProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string ErpProductCode { get; init; } = string.Empty;
    public decimal? SalePrice { get; init; }
    public bool IsEnabled { get; init; } = true;
}

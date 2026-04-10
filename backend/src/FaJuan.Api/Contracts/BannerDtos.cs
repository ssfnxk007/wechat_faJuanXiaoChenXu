namespace FaJuan.Api.Contracts;

public class BannerListItemDto
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public long ImageAssetId { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string? LinkUrl { get; init; }
    public int Sort { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class SaveBannerRequest
{
    public string Title { get; init; } = string.Empty;
    public long ImageAssetId { get; init; }
    public string? LinkUrl { get; init; }
    public int Sort { get; init; }
    public bool IsEnabled { get; init; } = true;
}

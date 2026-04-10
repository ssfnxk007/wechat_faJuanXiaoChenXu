namespace FaJuan.Api.Domain.Entities;

public class Banner
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long ImageAssetId { get; set; }
    public string? LinkUrl { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

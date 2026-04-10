namespace FaJuan.Api.Domain.Entities;

public class MediaAsset
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image";
    public string BucketType { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

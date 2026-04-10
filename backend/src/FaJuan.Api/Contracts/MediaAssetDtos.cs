namespace FaJuan.Api.Contracts;

public class MediaAssetListItemDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string MediaType { get; init; } = string.Empty;
    public string BucketType { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; init; } = [];
    public int Sort { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class SaveMediaAssetRequest
{
    public string Name { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string MediaType { get; init; } = "image";
    public string BucketType { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; init; } = [];
    public int Sort { get; init; }
    public bool IsEnabled { get; init; } = true;
}

public class MediaAssetUploadResultDto
{
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
}

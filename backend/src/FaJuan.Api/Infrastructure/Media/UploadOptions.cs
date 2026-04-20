namespace FaJuan.Api.Infrastructure.Media;

public class UploadOptions
{
    public long MaxFileSizeBytes { get; init; } = 10_485_760;

    public int DefaultLongSide { get; init; } = 1080;

    public int BannerLongSide { get; init; } = 1200;

    public int WebpQuality { get; init; } = 82;
}

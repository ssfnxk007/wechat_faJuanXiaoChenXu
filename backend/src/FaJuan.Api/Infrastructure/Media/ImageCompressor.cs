using Microsoft.Extensions.Options;
using SkiaSharp;

namespace FaJuan.Api.Infrastructure.Media;

public class ImageCompressor(IOptions<UploadOptions> options)
{
    private readonly UploadOptions _options = options.Value;

    public int GetLongSideLimit(string? bucketType)
    {
        return string.Equals(bucketType?.Trim(), "banner", StringComparison.OrdinalIgnoreCase)
            ? _options.BannerLongSide
            : _options.DefaultLongSide;
    }

    public async Task<byte[]> CompressToWebpAsync(Stream sourceStream, int longSideLimit, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await sourceStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        using var original = SKBitmap.Decode(buffer)
            ?? throw new InvalidOperationException("图片解码失败，可能文件损坏或格式不支持");

        var (targetWidth, targetHeight) = ComputeTargetSize(original.Width, original.Height, longSideLimit);

        SKBitmap working;
        var ownsWorking = false;
        if (targetWidth == original.Width && targetHeight == original.Height)
        {
            working = original;
        }
        else
        {
            working = original.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.High)
                ?? throw new InvalidOperationException("图片缩放失败");
            ownsWorking = true;
        }

        try
        {
            using var image = SKImage.FromBitmap(working);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, _options.WebpQuality)
                ?? throw new InvalidOperationException("图片编码失败");
            return encoded.ToArray();
        }
        finally
        {
            if (ownsWorking)
            {
                working.Dispose();
            }
        }
    }

    private static (int Width, int Height) ComputeTargetSize(int originalWidth, int originalHeight, int longSideLimit)
    {
        var longSide = Math.Max(originalWidth, originalHeight);
        if (longSide <= longSideLimit)
        {
            return (originalWidth, originalHeight);
        }

        var scale = (double)longSideLimit / longSide;
        var width = Math.Max(1, (int)Math.Round(originalWidth * scale));
        var height = Math.Max(1, (int)Math.Round(originalHeight * scale));
        return (width, height);
    }
}

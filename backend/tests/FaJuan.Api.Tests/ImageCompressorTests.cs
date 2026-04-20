using FaJuan.Api.Infrastructure.Media;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace FaJuan.Api.Tests;

public class ImageCompressorTests
{
    private static ImageCompressor CreateCompressor(UploadOptions? options = null)
    {
        return new ImageCompressor(Options.Create(options ?? new UploadOptions()));
    }

    private static byte[] CreatePng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Orange);
        using var paint = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Fill };
        canvas.DrawCircle(width / 2f, height / 2f, Math.Min(width, height) / 3f, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public async Task CompressToWebp_WhenLongSideExceedsLimit_DownscalesAndShrinks()
    {
        var source = CreatePng(2000, 1500);
        var compressor = CreateCompressor();

        byte[] compressed;
        using (var stream = new MemoryStream(source))
        {
            compressed = await compressor.CompressToWebpAsync(stream, longSideLimit: 1080);
        }

        Assert.NotEmpty(compressed);
        Assert.True(compressed.Length < source.Length, $"compressed {compressed.Length} should be smaller than source {source.Length}");

        using var decoded = SKBitmap.Decode(compressed);
        Assert.NotNull(decoded);
        Assert.True(Math.Max(decoded.Width, decoded.Height) <= 1080);
    }

    [Fact]
    public async Task CompressToWebp_WhenLongSideWithinLimit_KeepsOriginalDimensions()
    {
        var source = CreatePng(600, 400);
        var compressor = CreateCompressor();

        byte[] compressed;
        using (var stream = new MemoryStream(source))
        {
            compressed = await compressor.CompressToWebpAsync(stream, longSideLimit: 1080);
        }

        using var decoded = SKBitmap.Decode(compressed);
        Assert.Equal(600, decoded.Width);
        Assert.Equal(400, decoded.Height);
    }

    [Fact]
    public void GetLongSideLimit_PicksBannerOrDefaultByBucketType()
    {
        var compressor = CreateCompressor(new UploadOptions
        {
            DefaultLongSide = 800,
            BannerLongSide = 1600,
        });

        Assert.Equal(1600, compressor.GetLongSideLimit("banner"));
        Assert.Equal(1600, compressor.GetLongSideLimit("Banner"));
        Assert.Equal(800, compressor.GetLongSideLimit("product"));
        Assert.Equal(800, compressor.GetLongSideLimit(null));
        Assert.Equal(800, compressor.GetLongSideLimit(""));
    }

    [Fact]
    public async Task CompressToWebp_WithCorruptBytes_ThrowsInvalidOperation()
    {
        var compressor = CreateCompressor();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => compressor.CompressToWebpAsync(stream, longSideLimit: 1080));
    }
}

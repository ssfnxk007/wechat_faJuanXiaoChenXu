using System.Text.Json;
using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Media;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/products")]
public class MediaAssetsController(AppDbContext dbContext, IWebHostEnvironment environment) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MediaAssetListItemDto>>>> GetList(
        [FromQuery] string? bucketType,
        [FromQuery] string? keyword,
        [FromQuery] bool? isEnabled,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = dbContext.MediaAssets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(bucketType))
        {
            var normalizedBucketType = bucketType.Trim().ToLowerInvariant();
            query = query.Where(x => x.BucketType == normalizedBucketType);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x => x.Name.Contains(normalizedKeyword) || (x.Tags != null && x.Tags.Contains(normalizedKeyword)));
        }

        if (isEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == isEnabled.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Sort, false, x => x.Id, true)
            .Select(x => new MediaAssetListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                FileUrl = x.FileUrl,
                MediaType = x.MediaType,
                BucketType = x.BucketType,
                Tags = ParseTags(x.Tags),
                Sort = x.Sort,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        return Ok(Success(new PagedResult<MediaAssetListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("product.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveMediaAssetRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = new MediaAsset
        {
            Name = request.Name.Trim(),
            FileUrl = request.FileUrl.Trim(),
            MediaType = NormalizeMediaType(request.MediaType),
            BucketType = NormalizeBucketType(request.BucketType),
            Tags = SerializeTags(request.Tags),
            Sort = request.Sort,
            IsEnabled = request.IsEnabled,
        };

        dbContext.MediaAssets.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "创建成功"));
    }

    [AdminPermissionAuthorize("product.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveMediaAssetRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("素材不存在"));
        }

        entity.Name = request.Name.Trim();
        entity.FileUrl = request.FileUrl.Trim();
        entity.MediaType = NormalizeMediaType(request.MediaType);
        entity.BucketType = NormalizeBucketType(request.BucketType);
        entity.Tags = SerializeTags(request.Tags);
        entity.Sort = request.Sort;
        entity.IsEnabled = request.IsEnabled;

        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "更新成功"));
    }

    [AdminPermissionAuthorize("product.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("素材不存在"));
        }

        var usedByProductMainImage = await dbContext.Products.AnyAsync(x => x.MainImageAssetId == id);
        var productDetailImageAssetIds = await dbContext.Products.AsNoTracking()
            .Where(x => x.DetailImageAssetIds != null)
            .Select(x => x.DetailImageAssetIds)
            .ToListAsync();
        var usedByProductDetailImage = productDetailImageAssetIds.Any(value => ParseDetailImageAssetIds(value).Contains(id));
        var usedByTemplate = await dbContext.CouponTemplates.AnyAsync(x => x.ImageAssetId == id);
        var usedByPack = await dbContext.CouponPacks.AnyAsync(x => x.ImageAssetId == id);
        var usedByBanner = await dbContext.Banners.AnyAsync(x => x.ImageAssetId == id);
        if (usedByProductMainImage || usedByProductDetailImage || usedByTemplate || usedByPack || usedByBanner)
        {
            return BadRequest(Failure<bool>("素材已被业务数据引用，不能删除"));
        }

        dbContext.MediaAssets.Remove(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(true, "删除成功"));
    }

    [AdminPermissionAuthorize("product.create")]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<MediaAssetUploadResultDto>>> Upload(
        [FromForm] UploadMediaAssetRequest request,
        [FromServices] ImageCompressor imageCompressor,
        [FromServices] IOptions<UploadOptions> uploadOptions)
    {
        var file = request.File;
        var bucketType = request.BucketType;
        if (file is null || file.Length == 0)
        {
            return BadRequest(Failure<MediaAssetUploadResultDto>("上传文件不能为空"));
        }

        var options = uploadOptions.Value;
        if (file.Length > options.MaxFileSizeBytes)
        {
            var maxMb = Math.Max(1, options.MaxFileSizeBytes / 1024 / 1024);
            return BadRequest(Failure<MediaAssetUploadResultDto>($"单张图片不能超过 {maxMb} MB"));
        }

        var extension = Path.GetExtension(file.FileName);
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(Failure<MediaAssetUploadResultDto>("仅支持 jpg、jpeg、png、gif、webp 图片"));
        }

        byte[] compressed;
        try
        {
            await using var stream = file.OpenReadStream();
            var longSide = imageCompressor.GetLongSideLimit(bucketType);
            compressed = await imageCompressor.CompressToWebpAsync(stream, longSide, HttpContext.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Failure<MediaAssetUploadResultDto>(ex.Message));
        }

        var uploadRoot = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "media");
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.webp";
        var filePath = Path.Combine(uploadRoot, fileName);

        await System.IO.File.WriteAllBytesAsync(filePath, compressed, HttpContext.RequestAborted);

        var fileUrl = $"/uploads/media/{fileName}";
        return Ok(Success(new MediaAssetUploadResultDto
        {
            FileName = file.FileName,
            FileUrl = fileUrl,
        }, "上传成功"));
    }

    private static string? ValidateRequest(SaveMediaAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "素材名称不能为空";
        }

        if (string.IsNullOrWhiteSpace(request.FileUrl))
        {
            return "素材地址不能为空";
        }

        if (!IsSupportedBucketType(request.BucketType))
        {
            return "素材分区仅支持 product、banner、shared";
        }

        return null;
    }

    private static bool IsSupportedBucketType(string? value)
    {
        var normalized = NormalizeBucketType(value);
        return normalized is "product" or "banner" or "shared";
    }

    private static string NormalizeBucketType(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static string NormalizeMediaType(string? value)
        => string.IsNullOrWhiteSpace(value) ? "image" : value.Trim().ToLowerInvariant();

    private static string? SerializeTags(IReadOnlyCollection<string>? values)
    {
        if (values is null)
        {
            return null;
        }

        var normalized = values
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    private static IReadOnlyCollection<string> ParseTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyCollection<long> ParseDetailImageAssetIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<long[]>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

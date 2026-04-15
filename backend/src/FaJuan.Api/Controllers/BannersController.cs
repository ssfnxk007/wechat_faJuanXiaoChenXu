using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/banners")]
public class BannersController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BannerListItemDto>>>> GetList(
        [FromQuery] string? keyword,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = from banner in dbContext.Banners.AsNoTracking()
                    join asset in dbContext.MediaAssets.AsNoTracking() on banner.ImageAssetId equals asset.Id
                    select new BannerListItemDto
                    {
                        Id = banner.Id,
                        Title = banner.Title,
                        ImageAssetId = banner.ImageAssetId,
                        ImageUrl = asset.FileUrl,
                        LinkUrl = banner.LinkUrl,
                        Sort = banner.Sort,
                        IsEnabled = banner.IsEnabled,
                        CreatedAt = banner.CreatedAt,
                    };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x => x.Title.Contains(normalizedKeyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .ApplyLegacyPaging(pageIndex, pageSize, x => x.Sort, false, x => x.Id, true)
            .ToListAsync();

        return Ok(Success(new PagedResult<BannerListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("banner.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveBannerRequest request)
    {
        var validationError = await ValidateRequestAsync(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = new Banner
        {
            Title = request.Title.Trim(),
            ImageAssetId = request.ImageAssetId,
            LinkUrl = NormalizeOptionalUrl(request.LinkUrl),
            Sort = request.Sort,
            IsEnabled = request.IsEnabled,
        };

        dbContext.Banners.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "创建成功"));
    }

    [AdminPermissionAuthorize("banner.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveBannerRequest request)
    {
        var entity = await dbContext.Banners.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("轮播图不存在"));
        }

        var validationError = await ValidateRequestAsync(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        entity.Title = request.Title.Trim();
        entity.ImageAssetId = request.ImageAssetId;
        entity.LinkUrl = NormalizeOptionalUrl(request.LinkUrl);
        entity.Sort = request.Sort;
        entity.IsEnabled = request.IsEnabled;

        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "更新成功"));
    }

    [AdminPermissionAuthorize("banner.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.Banners.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("轮播图不存在"));
        }

        dbContext.Banners.Remove(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(true, "删除成功"));
    }

    private async Task<string?> ValidateRequestAsync(SaveBannerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "轮播图标题不能为空";
        }

        if (request.ImageAssetId <= 0)
        {
            return "轮播图图片素材不能为空";
        }

        var asset = await dbContext.MediaAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ImageAssetId);
        if (asset is null)
        {
            return "轮播图图片素材不存在";
        }

        if (asset.BucketType != "banner" && asset.BucketType != "shared")
        {
            return "轮播图只能引用 banner 或 shared 分区素材";
        }

        return null;
    }

    private static string? NormalizeOptionalUrl(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/products")]
public class ProductsController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword) || x.ErpProductCode.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ErpProductCode,
                x.MainImageAssetId,
                x.DetailImageAssetIds,
                x.SalePrice,
                x.IsEnabled,
                x.CreatedAt,
            })
            .ToListAsync();

        var assetIds = items
            .SelectMany(x => ParseDetailImageAssetIds(x.DetailImageAssetIds).Concat(x.MainImageAssetId.HasValue ? [x.MainImageAssetId.Value] : []))
            .Distinct()
            .ToArray();

        var assetMap = assetIds.Length == 0
            ? new Dictionary<long, string>()
            : await dbContext.MediaAssets.AsNoTracking()
                .Where(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileUrl);

        var resultItems = items.Select(x =>
        {
            var detailAssetIds = ParseDetailImageAssetIds(x.DetailImageAssetIds);

            return new ProductListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                ErpProductCode = x.ErpProductCode,
                MainImageAssetId = x.MainImageAssetId,
                MainImageUrl = x.MainImageAssetId.HasValue && assetMap.TryGetValue(x.MainImageAssetId.Value, out var mainImageUrl) ? mainImageUrl : null,
                DetailImageAssetIds = detailAssetIds,
                DetailImageUrls = detailAssetIds.Where(assetMap.ContainsKey).Select(assetId => assetMap[assetId]).ToArray(),
                SalePrice = x.SalePrice,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
            };
        }).ToList();

        return Ok(Success(new PagedResult<ProductListItemDto>
        {
            Items = resultItems,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [AdminPermissionAuthorize("product.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveProductRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var normalizedCode = request.ErpProductCode.Trim();
        var exists = await dbContext.Products.AnyAsync(x => x.ErpProductCode == normalizedCode);
        if (exists)
        {
            return BadRequest(Failure<long>("ERP 商品编码已存在"));
        }

        var entity = new Product
        {
            Name = request.Name.Trim(),
            ErpProductCode = normalizedCode,
            MainImageAssetId = request.MainImageAssetId,
            DetailImageAssetIds = SerializeDetailImageAssetIds(request.DetailImageAssetIds),
            SalePrice = request.SalePrice,
            IsEnabled = request.IsEnabled,
        };

        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "创建成功"));
    }

    [AdminPermissionAuthorize("product.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveProductRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("商品不存在"));
        }

        var normalizedCode = request.ErpProductCode.Trim();
        var duplicatedCode = await dbContext.Products.AnyAsync(x => x.Id != id && x.ErpProductCode == normalizedCode);
        if (duplicatedCode)
        {
            return BadRequest(Failure<long>("ERP 商品编码已存在"));
        }

        entity.Name = request.Name.Trim();
        entity.ErpProductCode = normalizedCode;
        entity.MainImageAssetId = request.MainImageAssetId;
        entity.DetailImageAssetIds = SerializeDetailImageAssetIds(request.DetailImageAssetIds);
        entity.SalePrice = request.SalePrice;
        entity.IsEnabled = request.IsEnabled;

        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "更新成功"));
    }

    [AdminPermissionAuthorize("product.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("商品不存在"));
        }

        dbContext.Products.Remove(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(true, "删除成功"));
    }

    private static string? ValidateRequest(SaveProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ErpProductCode))
        {
            return "商品名称和 ERP 商品编码不能为空";
        }

        if (request.MainImageAssetId.HasValue && request.MainImageAssetId.Value <= 0)
        {
            return "商品主图素材无效";
        }

        return null;
    }

    private static string? SerializeDetailImageAssetIds(IReadOnlyCollection<long>? values)
    {
        if (values is null)
        {
            return null;
        }

        var normalized = values
            .Where(x => x > 0)
            .Distinct()
            .ToArray();

        return normalized.Length == 0 ? null : JsonSerializer.Serialize(normalized);
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

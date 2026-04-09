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
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                ErpProductCode = x.ErpProductCode,
                SalePrice = x.SalePrice,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        return Ok(Success(new PagedResult<ProductListItemDto>
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

        return null;
    }
}

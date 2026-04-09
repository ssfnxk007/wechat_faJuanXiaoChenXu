using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/coupon-pack-items")]
public class CouponPackItemsController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet("{couponPackId:long}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CouponPackItemDto>>>> GetList(long couponPackId)
    {
        var items = await dbContext.CouponPackItems.AsNoTracking()
            .Where(x => x.CouponPackId == couponPackId)
            .Join(dbContext.CouponTemplates.AsNoTracking(),
                item => item.CouponTemplateId,
                template => template.Id,
                (item, template) => new CouponPackItemDto
                {
                    Id = item.Id,
                    CouponPackId = item.CouponPackId,
                    CouponTemplateId = item.CouponTemplateId,
                    Quantity = item.Quantity,
                    CouponTemplateName = template.Name,
                })
            .ToListAsync();

        return Ok(Success<IReadOnlyCollection<CouponPackItemDto>>(items));
    }

    [AdminPermissionAuthorize("coupon-pack-item.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] SaveCouponPackItemRequest request)
    {
        var validationError = await ValidateRequestAsync(request);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var exists = await dbContext.CouponPackItems.FirstOrDefaultAsync(x => x.CouponPackId == request.CouponPackId && x.CouponTemplateId == request.CouponTemplateId);
        if (exists is not null)
        {
            exists.Quantity = request.Quantity;
            await dbContext.SaveChangesAsync();
            return Ok(Success(exists.Id, "更新成功"));
        }

        var entity = new Domain.Entities.CouponPackItem
        {
            CouponPackId = request.CouponPackId,
            CouponTemplateId = request.CouponTemplateId,
            Quantity = request.Quantity,
        };

        dbContext.CouponPackItems.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "创建成功"));
    }

    [AdminPermissionAuthorize("coupon-pack-item.edit")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<long>>> Update(long id, [FromBody] SaveCouponPackItemRequest request)
    {
        var validationError = await ValidateRequestAsync(request, id);
        if (validationError is not null)
        {
            return BadRequest(Failure<long>(validationError));
        }

        var entity = await dbContext.CouponPackItems.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<long>("券包明细不存在"));
        }

        entity.CouponPackId = request.CouponPackId;
        entity.CouponTemplateId = request.CouponTemplateId;
        entity.Quantity = request.Quantity;

        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "更新成功"));
    }

    [AdminPermissionAuthorize("coupon-pack-item.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(long id)
    {
        var entity = await dbContext.CouponPackItems.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(Failure<bool>("券包明细不存在"));
        }

        dbContext.CouponPackItems.Remove(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(true, "删除成功"));
    }

    private async Task<string?> ValidateRequestAsync(SaveCouponPackItemRequest request, long? currentId = null)
    {
        if (request.CouponPackId <= 0 || request.CouponTemplateId <= 0 || request.Quantity <= 0)
        {
            return "券包、券模板和数量必须有效";
        }

        var packExists = await dbContext.CouponPacks.AnyAsync(x => x.Id == request.CouponPackId);
        var templateExists = await dbContext.CouponTemplates.AnyAsync(x => x.Id == request.CouponTemplateId);
        if (!packExists || !templateExists)
        {
            return "券包或券模板不存在";
        }

        var duplicatedItem = await dbContext.CouponPackItems.AnyAsync(x =>
            x.Id != currentId &&
            x.CouponPackId == request.CouponPackId &&
            x.CouponTemplateId == request.CouponTemplateId);
        if (duplicatedItem)
        {
            return "同一券包下的券模板已存在";
        }

        return null;
    }
}

using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

public class WriteOffController(AppDbContext dbContext) : ApiControllerBase
{
    [AdminPermissionAuthorize("writeoff.execute")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CouponWriteOffResultDto>>> WriteOff([FromBody] CouponWriteOffRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CouponCode) || request.StoreId <= 0)
        {
            return BadRequest(Failure<CouponWriteOffResultDto>("券码和门店不能为空"));
        }

        var coupon = await dbContext.UserCoupons.FirstOrDefaultAsync(x => x.CouponCode == request.CouponCode.Trim());
        if (coupon is null)
        {
            return NotFound(Failure<CouponWriteOffResultDto>("券不存在", 404));
        }

        var template = await dbContext.CouponTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == coupon.CouponTemplateId);
        if (template is null)
        {
            return BadRequest(Failure<CouponWriteOffResultDto>("券模板不存在或已删除"));
        }

        if (coupon.Status == UserCouponStatus.Used)
        {
            return BadRequest(Failure<CouponWriteOffResultDto>("券已核销"));
        }

        if (coupon.Status == UserCouponStatus.Voided)
        {
            return BadRequest(Failure<CouponWriteOffResultDto>("券已作废"));
        }

        var now = DateTime.Now;
        if (coupon.EffectiveAt > now)
        {
            return BadRequest(Failure<CouponWriteOffResultDto>("券未到生效时间"));
        }

        if (coupon.ExpireAt < now)
        {
            coupon.Status = UserCouponStatus.Expired;
            await dbContext.SaveChangesAsync();
            return BadRequest(Failure<CouponWriteOffResultDto>("券已过期"));
        }

        var storeExists = await dbContext.Stores.AnyAsync(x => x.Id == request.StoreId && x.IsEnabled);
        if (!storeExists)
        {
            return BadRequest(Failure<CouponWriteOffResultDto>("门店不存在或已停用"));
        }

        if (!template.IsAllStores)
        {
            var storeInScope = await dbContext.CouponTemplateStoreScopes.AsNoTracking()
                .AnyAsync(x => x.CouponTemplateId == template.Id && x.StoreId == request.StoreId);

            if (!storeInScope)
            {
                return BadRequest(Failure<CouponWriteOffResultDto>("当前门店不在该券适用范围内"));
            }
        }

        if (template.TemplateType == CouponTemplateType.Product)
        {
            if (!request.ProductId.HasValue || request.ProductId.Value <= 0)
            {
                return BadRequest(Failure<CouponWriteOffResultDto>("指定商品券核销时必须提供商品ID"));
            }

            var inScope = await dbContext.CouponTemplateProductScopes.AsNoTracking()
                .AnyAsync(x => x.CouponTemplateId == template.Id && x.ProductId == request.ProductId.Value);

            if (!inScope)
            {
                return BadRequest(Failure<CouponWriteOffResultDto>("该商品不在券适用范围内"));
            }
        }

        coupon.Status = UserCouponStatus.Used;
        if (template.TemplateType == CouponTemplateType.Product)
        {
            coupon.FulfillmentStatus = CouponFulfillmentStatus.Fulfilled;
        }

        dbContext.Set<CouponWriteOffRecord>().Add(new CouponWriteOffRecord
        {
            UserCouponId = coupon.Id,
            CouponCode = coupon.CouponCode,
            StoreId = request.StoreId,
            ProductId = request.ProductId,
            OperatorName = request.OperatorName?.Trim(),
            DeviceCode = request.DeviceCode?.Trim(),
            WriteOffAt = now,
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // 并发核销：另一笔请求已写入 Used，此处直接返回"已核销"
            return BadRequest(Failure<CouponWriteOffResultDto>("券已核销"));
        }

        return Ok(Success(new CouponWriteOffResultDto
        {
            UserCouponId = coupon.Id,
            CouponCode = coupon.CouponCode,
            AppUserId = coupon.AppUserId,
            CouponTemplateId = coupon.CouponTemplateId,
            Message = "核销成功",
        }));
    }
}

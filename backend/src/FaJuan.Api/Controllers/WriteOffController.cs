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

        coupon.Status = UserCouponStatus.Used;
        dbContext.Set<CouponWriteOffRecord>().Add(new CouponWriteOffRecord
        {
            UserCouponId = coupon.Id,
            CouponCode = coupon.CouponCode,
            StoreId = request.StoreId,
            OperatorName = request.OperatorName?.Trim(),
            DeviceCode = request.DeviceCode?.Trim(),
            WriteOffAt = now,
        });
        await dbContext.SaveChangesAsync();

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

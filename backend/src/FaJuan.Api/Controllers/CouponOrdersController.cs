using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Common.Models;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/coupon-orders")]
public class CouponOrdersController(AppDbContext dbContext) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CouponOrderListItemDto>>>> GetList([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.CouponOrders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x => x.OrderNo.Contains(normalizedKeyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query.ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)
            .Select(x => new CouponOrderListItemDto
            {
                Id = x.Id,
                OrderNo = x.OrderNo,
                AppUserId = x.AppUserId,
                CouponPackId = x.CouponPackId,
                OrderAmount = x.OrderAmount,
                Status = (int)x.Status,
                PaidAt = x.PaidAt,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        return Ok(Success(new PagedResult<CouponOrderListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<CouponOrderDetailDto>>> GetDetail(long id)
    {
        var order = await dbContext.CouponOrders.AsNoTracking()
            .Where(x => x.Id == id)
            .Join(
                dbContext.CouponPacks.AsNoTracking(),
                couponOrder => couponOrder.CouponPackId,
                couponPack => couponPack.Id,
                (couponOrder, couponPack) => new
                {
                    Order = couponOrder,
                    CouponPackName = couponPack.Name,
                })
            .FirstOrDefaultAsync();

        if (order is null)
        {
            return NotFound(Failure<CouponOrderDetailDto>("订单不存在"));
        }

        var payments = await dbContext.PaymentTransactions.AsNoTracking()
            .Where(x => x.CouponOrderId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CouponOrderPaymentDto
            {
                Id = x.Id,
                PaymentNo = x.PaymentNo,
                Amount = x.Amount,
                Status = (int)x.Status,
                ChannelTradeNo = x.ChannelTradeNo,
                RawCallback = x.RawCallback,
                PaidAt = x.PaidAt,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync();

        var grantedCoupons = await dbContext.UserCoupons.AsNoTracking()
            .Where(x => x.CouponOrderId == id)
            .Join(
                dbContext.CouponTemplates.AsNoTracking(),
                userCoupon => userCoupon.CouponTemplateId,
                couponTemplate => couponTemplate.Id,
                (userCoupon, couponTemplate) => new
                {
                    userCoupon.Id,
                    userCoupon.CouponTemplateId,
                    CouponTemplateName = couponTemplate.Name,
                    TemplateType = (int)couponTemplate.TemplateType,
                    couponTemplate.DiscountAmount,
                    couponTemplate.ThresholdAmount,
                    couponTemplate.IsAllStores,
                    couponTemplate.IsNewUserOnly,
                    userCoupon.CouponCode,
                    Status = (int)userCoupon.Status,
                    userCoupon.ReceivedAt,
                    userCoupon.EffectiveAt,
                    userCoupon.ExpireAt,
                    userCoupon.CreatedAt,
                })
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CouponOrderGrantedCouponDto
            {
                Id = x.Id,
                CouponTemplateId = x.CouponTemplateId,
                CouponTemplateName = x.CouponTemplateName,
                TemplateType = x.TemplateType,
                DiscountAmount = x.DiscountAmount,
                ThresholdAmount = x.ThresholdAmount,
                IsAllStores = x.IsAllStores,
                IsNewUserOnly = x.IsNewUserOnly,
                CouponCode = x.CouponCode,
                Status = x.Status,
                ReceivedAt = x.ReceivedAt,
                EffectiveAt = x.EffectiveAt,
                ExpireAt = x.ExpireAt,
            })
            .ToListAsync();

        return Ok(Success(new CouponOrderDetailDto
        {
            Id = order.Order.Id,
            OrderNo = order.Order.OrderNo,
            AppUserId = order.Order.AppUserId,
            CouponPackId = order.Order.CouponPackId,
            CouponPackName = order.CouponPackName,
            OrderAmount = order.Order.OrderAmount,
            Status = (int)order.Order.Status,
            PaymentNo = order.Order.PaymentNo,
            PaidAt = order.Order.PaidAt,
            CreatedAt = order.Order.CreatedAt,
            Payments = payments,
            GrantedCoupons = grantedCoupons,
        }));
    }

    [AdminPermissionAuthorize("coupon-order.create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create([FromBody] CreateCouponOrderRequest request)
    {
        var user = await dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UserId);
        if (user is null)
        {
            return BadRequest(Failure<long>("用户不存在"));
        }

        var pack = await dbContext.CouponPacks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CouponPackId && x.Status == CouponPackStatus.Enabled);
        if (pack is null)
        {
            return BadRequest(Failure<long>("券包不存在或已停用"));
        }

        var entity = new CouponOrder
        {
            OrderNo = $"CP{DateTime.Now:yyyyMMddHHmmssfff}",
            AppUserId = request.UserId,
            CouponPackId = request.CouponPackId,
            OrderAmount = pack.SalePrice,
            Status = CouponOrderStatus.PendingPayment,
        };

        dbContext.CouponOrders.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(Success(entity.Id, "下单成功"));
    }
}

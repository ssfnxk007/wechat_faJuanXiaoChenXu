using FaJuan.Api.Application.Common;
using FaJuan.Api.Application.Orders;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/coupon-orders")]
public class PaymentsController(
    AppDbContext dbContext,
    OrderPaymentService orderPaymentService,
    WeChatPayService weChatPayService) : ApiControllerBase
{
    [HttpGet("wechat-status")]
    public async Task<ActionResult<ApiResponse<WeChatPayConfigStatusDto>>> GetWeChatPayStatus(CancellationToken cancellationToken)
    {
        return Ok(Success(await weChatPayService.GetStatusAsync(cancellationToken)));
    }

    [AdminPermissionAuthorize("coupon-order.pay")]
    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<CreatePaymentResultDto>>> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var order = await dbContext.CouponOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);
        if (order is null)
        {
            return BadRequest(Failure<CreatePaymentResultDto>("订单不存在"));
        }

        var user = await dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.AppUserId, cancellationToken);
        if (user is null)
        {
            return BadRequest(Failure<CreatePaymentResultDto>("订单所属用户不存在"));
        }

        var transaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(
            x => x.CouponOrderId == request.OrderId && x.Status == PaymentStatus.Pending,
            cancellationToken);

        if (transaction is null)
        {
            transaction = new PaymentTransaction
            {
                CouponOrderId = order.Id,
                PaymentNo = OrderNoGenerator.Create("PAY"),
                Amount = order.OrderAmount,
                Status = PaymentStatus.Pending,
            };
            dbContext.PaymentTransactions.Add(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var payStatus = await weChatPayService.GetStatusAsync(cancellationToken);
        if (!payStatus.IsConfigured)
        {
            if (!payStatus.EnableMockFallback)
            {
                return BadRequest(Failure<CreatePaymentResultDto>("微信支付未配置完成，且已关闭模拟支付回退"));
            }

            return Ok(Success(new CreatePaymentResultDto
            {
                PaymentTransactionId = transaction.Id,
                PaymentNo = transaction.PaymentNo,
                Amount = transaction.Amount,
                IsMock = true,
                MockPayToken = $"mock-pay-{transaction.PaymentNo}",
            }, "当前使用模拟支付"));
        }

        if (string.IsNullOrWhiteSpace(user.MiniOpenId))
        {
            return BadRequest(Failure<CreatePaymentResultDto>("用户缺少小程序 OpenId，无法发起 JSAPI 支付"));
        }

        var payResult = await weChatPayService.CreateJsapiOrderAsync(
            transaction.PaymentNo,
            $"券包订单-{order.OrderNo}",
            transaction.Amount,
            user.MiniOpenId,
            cancellationToken);

        if (!payResult.Success || payResult.Result is null)
        {
            return BadRequest(Failure<CreatePaymentResultDto>(payResult.Message));
        }

        var result = new CreatePaymentResultDto
        {
            PaymentTransactionId = transaction.Id,
            PaymentNo = transaction.PaymentNo,
            Amount = transaction.Amount,
            IsMock = payResult.Result.IsMock,
            MockPayToken = payResult.Result.MockPayToken,
            PrepayId = payResult.Result.PrepayId,
            TimeStamp = payResult.Result.TimeStamp,
            NonceStr = payResult.Result.NonceStr,
            PackageValue = payResult.Result.PackageValue,
            SignType = payResult.Result.SignType,
            PaySign = payResult.Result.PaySign,
        };

        return Ok(Success(result, payResult.Message));
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var timestamp = Request.Headers["Wechatpay-Timestamp"].FirstOrDefault();
        var nonce = Request.Headers["Wechatpay-Nonce"].FirstOrDefault();
        var signature = Request.Headers["Wechatpay-Signature"].FirstOrDefault();
        var serial = Request.Headers["Wechatpay-Serial"].FirstOrDefault();

        var isValid = await weChatPayService.VerifyCallbackSignatureAsync(serial ?? string.Empty, timestamp ?? string.Empty, nonce ?? string.Empty, signature ?? string.Empty, rawBody, cancellationToken);
        if (!isValid)
        {
            return BadRequest(new { code = "FAIL", message = "签名验证失败" });
        }

        var transactionResource = await weChatPayService.TryDecryptCallbackAsync(rawBody, cancellationToken);
        if (transactionResource is null)
        {
            return BadRequest(new { code = "FAIL", message = "回调解密失败或配置缺失" });
        }

        if (!string.Equals(transactionResource.TradeState, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { code = "SUCCESS", message = "忽略非成功支付通知" });
        }

        var transaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(x => x.PaymentNo == transactionResource.OutTradeNo, cancellationToken);
        if (transaction is null)
        {
            return NotFound(new { code = "FAIL", message = "支付流水不存在" });
        }

        var result = await orderPaymentService.MarkOrderPaidAsync(transaction, transactionResource.TransactionId, rawBody);
        if (!result.Success)
        {
            return BadRequest(new { code = "FAIL", message = result.Message });
        }

        return Ok(new { code = "SUCCESS", message = "成功" });
    }

    [AdminPermissionAuthorize("coupon-order.pay")]
    [HttpPost("callback/mock")]
    public async Task<ActionResult<ApiResponse<bool>>> MockCallback([FromBody] PaymentCallbackRequest request)
    {
        var transaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(x => x.PaymentNo == request.PaymentNo);
        if (transaction is null)
        {
            return NotFound(Failure<bool>("支付流水不存在", 404));
        }

        if (!request.Success)
        {
            transaction.Status = PaymentStatus.Failed;
            transaction.RawCallback = request.RawCallback;
            await dbContext.SaveChangesAsync();
            return BadRequest(Failure<bool>("支付失败"));
        }

        var result = await orderPaymentService.MarkOrderPaidAsync(transaction, request.ChannelTradeNo, request.RawCallback);
        if (!result.Success)
        {
            return BadRequest(Failure<bool>(result.Message));
        }

        return Ok(Success(true, result.Message));
    }

    [AdminPermissionAuthorize("coupon-order.refund")]
    [HttpPost("refund")]
    public async Task<ActionResult<ApiResponse<bool>>> Refund([FromBody] RefundOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await dbContext.CouponOrders.FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);
        if (order is null)
        {
            return NotFound(Failure<bool>("订单不存在", 404));
        }

        if (order.Status != CouponOrderStatus.Paid)
        {
            return BadRequest(Failure<bool>("仅已支付订单可申请退款"));
        }

        var transaction = await dbContext.PaymentTransactions
            .FirstOrDefaultAsync(x => x.CouponOrderId == order.Id && x.Status == PaymentStatus.Success, cancellationToken);
        if (transaction is null)
        {
            return BadRequest(Failure<bool>("未找到有效支付流水"));
        }

        var userCoupons = await dbContext.UserCoupons
            .Where(x => x.CouponOrderId == order.Id)
            .ToListAsync(cancellationToken);

        if (userCoupons.Count == 0)
        {
            return BadRequest(Failure<bool>("未找到该订单发放的用户券"));
        }

        if (userCoupons.Any(x => x.Status == UserCouponStatus.Used))
        {
            return BadRequest(Failure<bool>("存在已核销的券，无法退款"));
        }

        var refundResult = await weChatPayService.RefundAsync(transaction.PaymentNo, transaction.Amount, "运营退款", cancellationToken);
        if (!refundResult.Success)
        {
            return BadRequest(Failure<bool>(refundResult.Message));
        }

        order.Status = CouponOrderStatus.Refunded;
        transaction.Status = PaymentStatus.Refunded;

        foreach (var coupon in userCoupons)
        {
            coupon.Status = UserCouponStatus.Recycled;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(Success(true, "退款成功"));
    }
}

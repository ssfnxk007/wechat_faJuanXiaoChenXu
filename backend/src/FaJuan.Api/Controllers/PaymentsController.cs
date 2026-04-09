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
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/coupon-orders")]
public class PaymentsController(
    AppDbContext dbContext,
    OrderPaymentService orderPaymentService,
    WeChatPayService weChatPayService,
    IOptions<WeChatPayOptions> weChatPayOptions) : ApiControllerBase
{
    [HttpGet("wechat-status")]
    public ActionResult<ApiResponse<WeChatPayConfigStatusDto>> GetWeChatPayStatus()
    {
        return Ok(Success(weChatPayService.GetStatus()));
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
                PaymentNo = $"PAY{DateTime.Now:yyyyMMddHHmmssfff}",
                Amount = order.OrderAmount,
                Status = PaymentStatus.Pending,
            };
            dbContext.PaymentTransactions.Add(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!weChatPayService.IsConfigured())
        {
            if (!weChatPayOptions.Value.EnableMockFallback)
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
        var transactionResource = weChatPayService.TryDecryptCallback(rawBody);
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
}

using FaJuan.Api.Application.Erp;
using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Controllers;

public class ErpCouponsController(ErpCouponService erpCouponService) : ApiControllerBase
{
    [ErpApiKeyAuthorize]
    [HttpPost("~/api/erp/coupons/preview")]
    public async Task<ActionResult<ApiResponse<ErpCouponPreviewDto>>> Preview([FromBody] ErpCouponPreviewRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SiteCode) || string.IsNullOrWhiteSpace(request.CouponCode))
        {
            return BadRequest(Failure<ErpCouponPreviewDto>("站点编号和券码不能为空"));
        }

        var result = await erpCouponService.PreviewAsync(request, cancellationToken);
        return Ok(Success(result, result.Message));
    }

    [ErpApiKeyAuthorize]
    [HttpPost("~/api/erp/coupons/writeoff")]
    public async Task<ActionResult<ApiResponse<ErpCouponWriteOffResultDto>>> WriteOff([FromBody] ErpCouponWriteOffRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SiteCode) || string.IsNullOrWhiteSpace(request.CouponCode))
        {
            return BadRequest(Failure<ErpCouponWriteOffResultDto>("站点编号和券码不能为空"));
        }

        var result = await erpCouponService.WriteOffAsync(request, cancellationToken);
        if (!result.Success)
        {
            if (result.Code == 404)
            {
                return NotFound(Failure<ErpCouponWriteOffResultDto>(result.Message, result.Code));
            }

            return BadRequest(Failure<ErpCouponWriteOffResultDto>(result.Message, result.Code));
        }

        return Ok(Success(result.Data!, result.Message));
    }
}

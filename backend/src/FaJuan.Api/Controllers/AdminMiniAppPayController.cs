using System.Text.RegularExpressions;
using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.WeChatPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/miniapp-settings")]
public class AdminMiniAppPayController(WeChatPaySettingsProvider provider) : ApiControllerBase
{
    private static readonly Regex DigitsOnly = new("^[0-9]{1,32}$");

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminWeChatPaySettingsDto>>> Get(CancellationToken cancellationToken)
    {
        var snapshot = await provider.GetAsync(cancellationToken);
        return Ok(Success(ToDto(snapshot)));
    }

    [AdminPermissionAuthorize("miniapp.pay.manage")]
    [HttpPut]
    public async Task<ActionResult<ApiResponse<AdminWeChatPaySettingsDto>>> Update(
        [FromBody] SaveAdminWeChatPaySettingsRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<AdminWeChatPaySettingsDto>("请求体不能为空"));
        }

        if (!string.IsNullOrWhiteSpace(request.MerchantId) && !DigitsOnly.IsMatch(request.MerchantId.Trim()))
        {
            return BadRequest(Failure<AdminWeChatPaySettingsDto>("商户号必须为数字，长度 1-32"));
        }

        if (!string.IsNullOrWhiteSpace(request.NotifyUrl)
            && !request.NotifyUrl.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(Failure<AdminWeChatPaySettingsDto>("支付回调地址必须以 https:// 开头"));
        }

        if (!string.IsNullOrWhiteSpace(request.PrivateKeyPem)
            && request.PrivateKeyPem.Trim() != WeChatPaySettingsProvider.DisplayPlaceholder
            && (!request.PrivateKeyPem.Contains("BEGIN PRIVATE KEY") || !request.PrivateKeyPem.Contains("END PRIVATE KEY")))
        {
            return BadRequest(Failure<AdminWeChatPaySettingsDto>("商户私钥 PEM 格式错误，必须包含 BEGIN/END PRIVATE KEY 标记"));
        }

        var snapshot = await provider.SaveAsync(new WeChatPaySettingsUpdate
        {
            AppId = request.AppId,
            MerchantId = request.MerchantId,
            MerchantSerialNo = request.MerchantSerialNo,
            PrivateKeyPem = request.PrivateKeyPem,
            ApiV3Key = request.ApiV3Key,
            NotifyUrl = request.NotifyUrl,
            EnableMockFallback = request.EnableMockFallback,
        }, cancellationToken);

        return Ok(Success(ToDto(snapshot), "保存成功"));
    }

    private static AdminWeChatPaySettingsDto ToDto(WeChatPaySettingsSnapshot snapshot) => new()
    {
        AppId = snapshot.AppId,
        MerchantId = snapshot.MerchantId,
        MerchantSerialNo = snapshot.MerchantSerialNo,
        PrivateKeyDisplay = string.IsNullOrEmpty(snapshot.PrivateKeyPem) ? string.Empty : WeChatPaySettingsProvider.DisplayPlaceholder,
        ApiV3KeyDisplay = string.IsNullOrEmpty(snapshot.ApiV3Key) ? string.Empty : WeChatPaySettingsProvider.DisplayPlaceholder,
        NotifyUrl = snapshot.NotifyUrl,
        EnableMockFallback = snapshot.EnableMockFallback,
        IsConfigured = snapshot.IsConfigured,
        UpdatedAt = snapshot.UpdatedAt == default ? null : snapshot.UpdatedAt,
    };
}

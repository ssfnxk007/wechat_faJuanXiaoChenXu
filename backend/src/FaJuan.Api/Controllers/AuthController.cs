using FaJuan.Api.Application.UserCoupons;
using FaJuan.Api.Contracts;
using FaJuan.Api.Domain.Entities;
using FaJuan.Api.Domain.Enums;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using FaJuan.Api.Infrastructure.WeChat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Controllers;

[AllowAnonymous]
public class AuthController(
    AppDbContext dbContext,
    WeChatMiniProgramService weChatMiniProgramService,
    IOptions<WeChatMiniProgramOptions> weChatOptions,
    JwtTokenService jwtTokenService,
    UserCouponGrantService userCouponGrantService) : ApiControllerBase
{
    [HttpGet("wechat-status")]
    public ActionResult<ApiResponse<WeChatConfigStatusDto>> GetWeChatStatus()
    {
        var options = weChatOptions.Value;
        var isConfigured = !string.IsNullOrWhiteSpace(options.AppId) && !string.IsNullOrWhiteSpace(options.AppSecret);
        var appIdPreview = string.IsNullOrWhiteSpace(options.AppId)
            ? string.Empty
            : options.AppId.Length <= 6
                ? options.AppId
                : $"{options.AppId[..3]}***{options.AppId[^3..]}";

        return Ok(Success(new WeChatConfigStatusDto
        {
            IsConfigured = isConfigured,
            AppIdPreview = appIdPreview,
            Message = isConfigured ? "微信小程序配置已就绪" : "请先在 appsettings 中填写 WeChatMiniProgram:AppId 和 WeChatMiniProgram:AppSecret",
        }));
    }

    [HttpPost("mini-login")]
    public async Task<ActionResult<ApiResponse<AuthLoginResultDto>>> MiniLogin([FromBody] MiniProgramLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(Failure<AuthLoginResultDto>("登录 code 不能为空"));
        }

        var code2Session = await weChatMiniProgramService.Code2SessionAsync(request.Code.Trim(), cancellationToken);
        if (!code2Session.IsSuccess || string.IsNullOrWhiteSpace(code2Session.OpenId))
        {
            return BadRequest(Failure<AuthLoginResultDto>(code2Session.ErrorMessage ?? "微信登录失败"));
        }

        var user = await dbContext.AppUsers.FirstOrDefaultAsync(x => x.MiniOpenId == code2Session.OpenId, cancellationToken);
        var isNewUser = false;

        if (user is null)
        {
            user = new AppUser
            {
                MiniOpenId = code2Session.OpenId,
                UnionId = code2Session.UnionId,
                Nickname = request.Nickname?.Trim(),
            };
            dbContext.AppUsers.Add(user);
            isNewUser = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var changed = false;
            if (!string.IsNullOrWhiteSpace(code2Session.UnionId) && user.UnionId != code2Session.UnionId)
            {
                user.UnionId = code2Session.UnionId;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(request.Nickname) && user.Nickname != request.Nickname.Trim())
            {
                user.Nickname = request.Nickname.Trim();
                changed = true;
            }
            if (changed)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        var tokenResult = jwtTokenService.CreateMiniAppToken(user.Id);

        return Ok(Success(new AuthLoginResultDto
        {
            UserId = user.Id,
            MiniOpenId = user.MiniOpenId,
            Mobile = user.Mobile,
            IsNewUser = isNewUser,
            Token = tokenResult.AccessToken,
        }));
    }

    [HttpPost("bind-mobile")]
    public async Task<ActionResult<ApiResponse<bool>>> BindMobile([FromBody] BindMobileRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Mobile))
        {
            return BadRequest(Failure<bool>("用户和手机号不能为空"));
        }

        var user = await dbContext.AppUsers.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return NotFound(Failure<bool>("用户不存在", 404));
        }

        user.Mobile = request.Mobile.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        var pendingDetails = await dbContext.CouponIssueImportDetails
            .Where(x => x.Mobile == user.Mobile && x.Status == CouponIssueImportDetailStatus.PendingMatch)
            .ToListAsync(cancellationToken);

        foreach (var detail in pendingDetails)
        {
            var grantResult = await userCouponGrantService.GrantAsync(
                detail.CouponTemplateId,
                new[]
                {
                    new ManualGrantUserCouponInput
                    {
                        AppUserId = user.Id,
                        QuantityPerUser = detail.Quantity > 0 ? detail.Quantity : 1,
                    }
                });

            var item = grantResult.Items.FirstOrDefault();
            if (item is not null && item.Success)
            {
                detail.Status = CouponIssueImportDetailStatus.MatchedAndGranted;
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(Success(true, "绑定成功"));
    }
}

using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.MiniApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Controllers;

[Authorize]
[AdminMenuAuthorize("/miniapp-settings")]
public class AdminMiniAppThemeController(MiniAppThemeSettingsService miniAppThemeSettingsService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminMiniAppThemeSettingsDto>>> Get(CancellationToken cancellationToken)
    {
        var settings = await miniAppThemeSettingsService.GetAsync(cancellationToken);
        return Ok(Success(new AdminMiniAppThemeSettingsDto
        {
            ThemeCode = settings.ThemeCode,
        }));
    }

    [AdminPermissionAuthorize("miniapp.theme.manage")]
    [HttpPut]
    public async Task<ActionResult<ApiResponse<AdminMiniAppThemeSettingsDto>>> Update([FromBody] SaveAdminMiniAppThemeSettingsRequest request, CancellationToken cancellationToken)
    {
        var themeCode = MiniAppThemeCodes.Normalize(request.ThemeCode);
        var settings = await miniAppThemeSettingsService.SaveAsync(themeCode, cancellationToken);

        return Ok(Success(new AdminMiniAppThemeSettingsDto
        {
            ThemeCode = settings.ThemeCode,
        }, "保存成功"));
    }
}

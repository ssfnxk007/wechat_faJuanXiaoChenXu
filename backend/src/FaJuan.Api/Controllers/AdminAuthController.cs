using FaJuan.Api.Contracts;
using FaJuan.Api.Infrastructure.Auth;
using FaJuan.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Controllers;

[AllowAnonymous]
public class AdminAuthController(
    IConfiguration configuration,
    IHostEnvironment environment,
    JwtTokenService jwtTokenService,
    PasswordHashService passwordHashService,
    AppDbContext dbContext) : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AdminLoginResultDto>>> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var hasDbAdmins = await dbContext.AdminUsers.AsNoTracking().AnyAsync(cancellationToken);
        var adminUser = await dbContext.AdminUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == request.Username && x.IsEnabled, cancellationToken);

        if (adminUser is not null)
        {
            if (!passwordHashService.Verify(request.Password, adminUser.PasswordHash))
            {
                return Unauthorized(Failure<AdminLoginResultDto>("账号或密码错误", 401));
            }

            // 登录成功时发现旧格式（SHA256 裸哈希）→ 在线升级为 PBKDF2
            if (passwordHashService.NeedsRehash(adminUser.PasswordHash))
            {
                var tracked = await dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Id == adminUser.Id, cancellationToken);
                if (tracked is not null)
                {
                    tracked.PasswordHash = passwordHashService.Hash(request.Password);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            var dbToken = jwtTokenService.CreateAdminToken(adminUser.Username);
            return Ok(Success(new AdminLoginResultDto
            {
                AccessToken = dbToken.AccessToken,
                Username = adminUser.Username,
                ExpiresAt = dbToken.ExpiresAt,
            }, "登录成功"));
        }

        if (hasDbAdmins)
        {
            return Unauthorized(Failure<AdminLoginResultDto>("账号或密码错误", 401));
        }

        // 无任何管理员账号时的兜底：仅允许开发环境使用内置账号引导，避免生产环境裸奔
        if (!environment.IsDevelopment())
        {
            return Unauthorized(Failure<AdminLoginResultDto>(
                "后台尚未初始化管理员账号，请先执行 002-admin-auth-seed.sql", 401));
        }

        var username = configuration["AdminAuth:Username"] ?? "admin";
        var password = configuration["AdminAuth:Password"] ?? "123456";

        if (!string.Equals(request.Username?.Trim(), username, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Password, password, StringComparison.Ordinal))
        {
            return Unauthorized(Failure<AdminLoginResultDto>("账号或密码错误", 401));
        }

        var tokenResult = jwtTokenService.CreateAdminToken(username);
        return Ok(Success(new AdminLoginResultDto
        {
            AccessToken = tokenResult.AccessToken,
            Username = username,
            ExpiresAt = tokenResult.ExpiresAt,
        }, "登录成功"));
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<AdminAuthProfileDto>>> GetProfile(CancellationToken cancellationToken)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(Failure<AdminAuthProfileDto>("登录状态已失效", 401));
        }

        var adminUser = await dbContext.AdminUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

        if (adminUser is null)
        {
            var fallbackMenus = await dbContext.AdminMenus.AsNoTracking()
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.Sort)
                .ThenBy(x => x.Id)
                .Select(x => x.Path)
                .ToListAsync(cancellationToken);

            return Ok(Success(new AdminAuthProfileDto
            {
                UserId = 0,
                Username = username,
                DisplayName = username,
                IsFallbackAdmin = true,
                RoleCodes = new[] { "super-admin" },
                RoleNames = new[] { "超级管理员" },
                MenuPaths = fallbackMenus,
                PermissionCodes = await dbContext.AdminPermissions.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.Sort).ThenBy(x => x.Id).Select(x => x.Code).ToListAsync(cancellationToken),
            }));
        }

        var roleRows = await dbContext.AdminUserRoles.AsNoTracking()
            .Where(x => x.AdminUserId == adminUser.Id)
            .Join(
                dbContext.AdminRoles.AsNoTracking().Where(x => x.IsEnabled),
                userRole => userRole.AdminRoleId,
                role => role.Id,
                (userRole, role) => new
                {
                    role.Id,
                    role.Code,
                    role.Name,
                })
            .ToListAsync(cancellationToken);

        var roleIds = roleRows.Select(x => x.Id).Distinct().ToArray();
        IReadOnlyCollection<string> menuPaths;
        if (roleIds.Length == 0)
        {
            menuPaths = Array.Empty<string>();
        }
        else
        {
            menuPaths = await dbContext.AdminUserRoles.AsNoTracking()
                .Where(x => x.AdminUserId == adminUser.Id)
                .Join(
                    dbContext.AdminRoleMenus.AsNoTracking(),
                    userRole => userRole.AdminRoleId,
                    roleMenu => roleMenu.AdminRoleId,
                    (userRole, roleMenu) => roleMenu.AdminMenuId)
                .Join(
                    dbContext.AdminMenus.AsNoTracking().Where(x => x.IsEnabled),
                    menuId => menuId,
                    menu => menu.Id,
                    (_, menu) => menu.Path)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        IReadOnlyCollection<string> permissionCodes;
        if (roleIds.Length == 0)
        {
            permissionCodes = Array.Empty<string>();
        }
        else
        {
            permissionCodes = await dbContext.AdminUserRoles.AsNoTracking()
                .Where(x => x.AdminUserId == adminUser.Id)
                .Join(
                    dbContext.AdminRolePermissions.AsNoTracking(),
                    userRole => userRole.AdminRoleId,
                    rolePermission => rolePermission.AdminRoleId,
                    (userRole, rolePermission) => rolePermission.AdminPermissionId)
                .Join(
                    dbContext.AdminPermissions.AsNoTracking().Where(x => x.IsEnabled),
                    permissionId => permissionId,
                    permission => permission.Id,
                    (_, permission) => permission.Code)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        return Ok(Success(new AdminAuthProfileDto
        {
            UserId = adminUser.Id,
            Username = adminUser.Username,
            DisplayName = adminUser.DisplayName,
            IsFallbackAdmin = false,
            RoleCodes = roleRows.Select(x => x.Code).Distinct().ToArray(),
            RoleNames = roleRows.Select(x => x.Name).Distinct().ToArray(),
            MenuPaths = menuPaths,
            PermissionCodes = permissionCodes,
        }));
    }
}

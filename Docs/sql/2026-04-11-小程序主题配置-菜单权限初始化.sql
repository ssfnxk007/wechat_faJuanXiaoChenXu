-- 小程序主题配置：菜单、权限、角色绑定初始化
-- 兼容 SQL Server 2008 R2

SET NOCOUNT ON;

DECLARE @Now DATETIME;
SET @Now = GETDATE();

DECLARE @ParentMenuId BIGINT;
DECLARE @MenuId BIGINT;
DECLARE @PermissionId BIGINT;

SELECT @ParentMenuId = Id
FROM AdminMenu
WHERE [Path] = '/admin-users';

IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE [Path] = '/miniapp-settings')
BEGIN
    INSERT INTO AdminMenu (ParentId, [Name], [Path], Component, Sort, IsEnabled, CreatedAt)
    VALUES (@ParentMenuId, N'小程序主题', '/miniapp-settings', 'views/miniapp-setting/MiniAppSettingView.vue', 80, 1, @Now);
END;

SELECT @MenuId = Id
FROM AdminMenu
WHERE [Path] = '/miniapp-settings';

IF NOT EXISTS (SELECT 1 FROM AdminPermission WHERE Code = 'miniapp.theme.manage')
BEGIN
    INSERT INTO AdminPermission ([Name], Code, MenuPath, Sort, IsEnabled, CreatedAt)
    VALUES (N'小程序主题配置', 'miniapp.theme.manage', '/miniapp-settings', 80, 1, @Now);
END;

SELECT @PermissionId = Id
FROM AdminPermission
WHERE Code = 'miniapp.theme.manage';

IF @MenuId IS NOT NULL
BEGIN
    INSERT INTO AdminRoleMenu (AdminRoleId, AdminMenuId, CreatedAt)
    SELECT r.Id, @MenuId, @Now
    FROM AdminRole r
    WHERE r.IsEnabled = 1
      AND r.Code = 'super-admin'
      AND NOT EXISTS (
          SELECT 1
          FROM AdminRoleMenu rm
          WHERE rm.AdminRoleId = r.Id AND rm.AdminMenuId = @MenuId
      );
END;

IF @PermissionId IS NOT NULL
BEGIN
    INSERT INTO AdminRolePermission (AdminRoleId, AdminPermissionId, CreatedAt)
    SELECT r.Id, @PermissionId, @Now
    FROM AdminRole r
    WHERE r.IsEnabled = 1
      AND r.Code = 'super-admin'
      AND NOT EXISTS (
          SELECT 1
          FROM AdminRolePermission rp
          WHERE rp.AdminRoleId = r.Id AND rp.AdminPermissionId = @PermissionId
      );
END;

-- 支付参数配置：权限、角色绑定初始化
-- 兼容 SQL Server 2008 R2
-- 菜单沿用现有 /miniapp-settings，仅新增按钮权限

SET NOCOUNT ON;

DECLARE @Now DATETIME;
SET @Now = GETDATE();

DECLARE @PermissionId BIGINT;

IF NOT EXISTS (SELECT 1 FROM AdminPermission WHERE Code = 'miniapp.pay.manage')
BEGIN
    INSERT INTO AdminPermission ([Name], Code, MenuPath, Sort, IsEnabled, CreatedAt)
    VALUES (N'支付参数配置', 'miniapp.pay.manage', '/miniapp-settings', 81, 1, @Now);
END;

SELECT @PermissionId = Id
FROM AdminPermission
WHERE Code = 'miniapp.pay.manage';

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

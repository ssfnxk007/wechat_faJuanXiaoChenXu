-- 分享追踪：菜单初始化
-- 兼容 SQL Server 2008 R2

SET NOCOUNT ON;

DECLARE @Now DATETIME;
SET @Now = GETDATE();

DECLARE @ParentMenuId BIGINT;
DECLARE @ShareTrackingMenuId BIGINT;

SELECT TOP 1 @ParentMenuId = ParentId
FROM AdminMenu
WHERE [Path] = '/coupon-templates';

IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE [Path] = '/share-tracking')
BEGIN
    INSERT INTO AdminMenu (ParentId, [Name], [Path], Component, Sort, IsEnabled, CreatedAt)
    VALUES (@ParentMenuId, N'分享追踪', '/share-tracking', 'views/share-tracking/ShareTrackingView.vue', 42, 1, @Now);
END;

SELECT @ShareTrackingMenuId = Id
FROM AdminMenu
WHERE [Path] = '/share-tracking';

IF @ShareTrackingMenuId IS NOT NULL
BEGIN
    INSERT INTO AdminRoleMenu (AdminRoleId, AdminMenuId, CreatedAt)
    SELECT r.Id, @ShareTrackingMenuId, @Now
    FROM AdminRole r
    WHERE r.IsEnabled = 1
      AND r.Code = 'super_admin'
      AND NOT EXISTS (
          SELECT 1
          FROM AdminRoleMenu rm
          WHERE rm.AdminRoleId = r.Id
            AND rm.AdminMenuId = @ShareTrackingMenuId
      );
END;

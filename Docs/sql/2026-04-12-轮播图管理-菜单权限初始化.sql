-- 轮播图管理：菜单、权限、角色绑定初始化
-- 兼容 SQL Server 2008 R2

SET NOCOUNT ON;

DECLARE @Now DATETIME;
SET @Now = GETDATE();

DECLARE @ParentMenuId BIGINT;
DECLARE @BannerMenuId BIGINT;
DECLARE @PermissionCreateId BIGINT;
DECLARE @PermissionEditId BIGINT;
DECLARE @PermissionDeleteId BIGINT;

SELECT TOP 1 @ParentMenuId = ParentId
FROM AdminMenu
WHERE [Path] = '/products';

IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE [Path] = '/banners')
BEGIN
    INSERT INTO AdminMenu (ParentId, [Name], [Path], Component, Sort, IsEnabled, CreatedAt)
    VALUES (@ParentMenuId, N'轮播图管理', '/banners', 'views/banner/BannerView.vue', 35, 1, @Now);
END;

SELECT @BannerMenuId = Id
FROM AdminMenu
WHERE [Path] = '/banners';

IF NOT EXISTS (SELECT 1 FROM AdminPermission WHERE Code = 'banner.create')
BEGIN
    INSERT INTO AdminPermission ([Name], Code, MenuPath, Sort, IsEnabled, CreatedAt)
    VALUES (N'轮播图新增', 'banner.create', '/banners', 351, 1, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM AdminPermission WHERE Code = 'banner.edit')
BEGIN
    INSERT INTO AdminPermission ([Name], Code, MenuPath, Sort, IsEnabled, CreatedAt)
    VALUES (N'轮播图编辑', 'banner.edit', '/banners', 352, 1, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM AdminPermission WHERE Code = 'banner.delete')
BEGIN
    INSERT INTO AdminPermission ([Name], Code, MenuPath, Sort, IsEnabled, CreatedAt)
    VALUES (N'轮播图删除', 'banner.delete', '/banners', 353, 1, @Now);
END;

SELECT @PermissionCreateId = Id FROM AdminPermission WHERE Code = 'banner.create';
SELECT @PermissionEditId = Id FROM AdminPermission WHERE Code = 'banner.edit';
SELECT @PermissionDeleteId = Id FROM AdminPermission WHERE Code = 'banner.delete';

IF @BannerMenuId IS NOT NULL
BEGIN
    INSERT INTO AdminRoleMenu (AdminRoleId, AdminMenuId, CreatedAt)
    SELECT r.Id, @BannerMenuId, @Now
    FROM AdminRole r
    WHERE r.IsEnabled = 1
      AND r.Code = 'super_admin'
      AND NOT EXISTS (
          SELECT 1
          FROM AdminRoleMenu rm
          WHERE rm.AdminRoleId = r.Id
            AND rm.AdminMenuId = @BannerMenuId
      );
END;

IF @PermissionCreateId IS NOT NULL
BEGIN
    INSERT INTO AdminRolePermission (AdminRoleId, AdminPermissionId, CreatedAt)
    SELECT r.Id, @PermissionCreateId, @Now
    FROM AdminRole r
    WHERE r.IsEnabled = 1
      AND r.Code = 'super_admin'
      AND NOT EXISTS (
          SELECT 1
          FROM AdminRolePermission rp
          WHERE rp.AdminRoleId = r.Id
            AND rp.AdminPermissionId = @PermissionCreateId
      );
END;

IF @PermissionEditId IS NOT NULL
BEGIN
    INSERT INTO AdminRolePermission (AdminRoleId, AdminPermissionId, CreatedAt)
    SELECT r.Id, @PermissionEditId, @Now
    FROM AdminRole r
    WHERE r.IsEnabled = 1
      AND r.Code = 'super_admin'
      AND NOT EXISTS (
          SELECT 1
          FROM AdminRolePermission rp
          WHERE rp.AdminRoleId = r.Id
            AND rp.AdminPermissionId = @PermissionEditId
      );
END;

IF @PermissionDeleteId IS NOT NULL
BEGIN
    INSERT INTO AdminRolePermission (AdminRoleId, AdminPermissionId, CreatedAt)
    SELECT r.Id, @PermissionDeleteId, @Now
    FROM AdminRole r
    WHERE r.IsEnabled = 1
      AND r.Code = 'super_admin'
      AND NOT EXISTS (
          SELECT 1
          FROM AdminRolePermission rp
          WHERE rp.AdminRoleId = r.Id
            AND rp.AdminPermissionId = @PermissionDeleteId
      );
END;

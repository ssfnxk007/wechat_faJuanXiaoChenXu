SET NOCOUNT ON;

DECLARE @Now DATETIME = GETDATE();
DECLARE @SuperAdminRoleId BIGINT;

SELECT @SuperAdminRoleId = Id
FROM dbo.AdminRole
WHERE Code = 'super_admin';

IF @SuperAdminRoleId IS NULL
BEGIN
    INSERT INTO dbo.AdminRole (Name, Code, IsEnabled, CreatedAt)
    VALUES (N'超级管理员', 'super_admin', 1, @Now);

    SET @SuperAdminRoleId = SCOPE_IDENTITY();
END

DECLARE @Menus TABLE (
    Name NVARCHAR(100) NOT NULL,
    Path NVARCHAR(200) NOT NULL,
    Component NVARCHAR(200) NOT NULL,
    Sort INT NOT NULL,
    IsEnabled BIT NOT NULL
);

INSERT INTO @Menus (Name, Path, Component, Sort, IsEnabled)
VALUES
    (N'用户管理', '/users', 'UserView', 10, 1),
    (N'门店管理', '/stores', 'StoreView', 20, 1),
    (N'商品管理', '/products', 'ProductView', 30, 1),
    (N'轮播图管理', '/banners', 'views/banner/BannerView.vue', 35, 1),
    (N'券模板', '/coupon-templates', 'CouponTemplateView', 40, 1),
    (N'分享追踪', '/share-tracking', 'views/share-tracking/ShareTrackingView.vue', 42, 1),
    (N'券包管理', '/coupon-packs', 'CouponPackView', 50, 1),
    (N'券包明细', '/coupon-pack-items', 'CouponPackItemView', 60, 0),
    (N'订单管理', '/coupon-orders', 'CouponOrderView', 70, 1),
    (N'用户券', '/user-coupons', 'UserCouponView', 80, 1),
    (N'小程序主题', '/miniapp-settings', 'views/miniapp-setting/MiniAppSettingView.vue', 80, 1),
    (N'核销中心', '/writeoff', 'WriteOffView', 90, 1),
    (N'菜单管理', '/admin-menus', 'views/admin-menu/AdminMenuView.vue', 92, 1),
    (N'权限管理', '/admin-users', 'AdminUserView', 100, 1),
    (N'角色管理', '/admin-roles', 'AdminRoleView', 110, 1);

INSERT INTO dbo.AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt)
SELECT NULL, m.Name, m.Path, m.Component, m.Sort, m.IsEnabled, @Now
FROM @Menus m
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.AdminMenu existing
    WHERE existing.Path = m.Path
);

UPDATE existing
SET
    existing.Name = m.Name,
    existing.Component = m.Component,
    existing.Sort = m.Sort,
    existing.IsEnabled = m.IsEnabled
FROM dbo.AdminMenu existing
JOIN @Menus m ON m.Path = existing.Path;

DECLARE @Permissions TABLE (
    Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(100) NOT NULL,
    MenuPath NVARCHAR(200) NOT NULL,
    Sort INT NOT NULL
);

INSERT INTO @Permissions (Name, Code, MenuPath, Sort)
VALUES
    (N'新增管理员', 'admin.user.create', '/admin-users', 10),
    (N'编辑管理员', 'admin.user.edit', '/admin-users', 20),
    (N'重置管理员密码', 'admin.user.reset-password', '/admin-users', 30),
    (N'删除管理员', 'admin.user.delete', '/admin-users', 40),
    (N'新增角色', 'admin.role.create', '/admin-roles', 10),
    (N'编辑角色', 'admin.role.edit', '/admin-roles', 20),
    (N'删除角色', 'admin.role.delete', '/admin-roles', 30),
    (N'新增菜单', 'admin.menu.create', '/admin-menus', 10),
    (N'编辑菜单', 'admin.menu.edit', '/admin-menus', 20),
    (N'删除菜单', 'admin.menu.delete', '/admin-menus', 30),
    (N'新增门店', 'store.create', '/stores', 10),
    (N'编辑门店', 'store.edit', '/stores', 20),
    (N'删除门店', 'store.delete', '/stores', 30),
    (N'新增商品', 'product.create', '/products', 10),
    (N'编辑商品', 'product.edit', '/products', 20),
    (N'删除商品', 'product.delete', '/products', 30),
    (N'新增券模板', 'coupon-template.create', '/coupon-templates', 10),
    (N'编辑券模板', 'coupon-template.edit', '/coupon-templates', 20),
    (N'删除券模板', 'coupon-template.delete', '/coupon-templates', 30),
    (N'新增券包', 'coupon-pack.create', '/coupon-packs', 10),
    (N'编辑券包', 'coupon-pack.edit', '/coupon-packs', 20),
    (N'删除券包', 'coupon-pack.delete', '/coupon-packs', 30),
    (N'新增券包明细', 'coupon-pack-item.create', '/coupon-pack-items', 10),
    (N'编辑券包明细', 'coupon-pack-item.edit', '/coupon-pack-items', 20),
    (N'删除券包明细', 'coupon-pack-item.delete', '/coupon-pack-items', 30),
    (N'创建订单', 'coupon-order.create', '/coupon-orders', 10),
    (N'发起支付', 'coupon-order.pay', '/coupon-orders', 20),
    (N'订单退款', 'coupon-order.refund', '/coupon-orders', 30),
    (N'执行核销', 'writeoff.execute', '/writeoff', 10),
    (N'手动发券', 'user-coupon.grant', '/user-coupons', 10),
    (N'小程序主题配置', 'miniapp.theme.manage', '/miniapp-settings', 80),
    (N'支付参数配置', 'miniapp.pay.manage', '/miniapp-settings', 81),
    (N'轮播图新增', 'banner.create', '/banners', 351),
    (N'轮播图编辑', 'banner.edit', '/banners', 352),
    (N'轮播图删除', 'banner.delete', '/banners', 353);

INSERT INTO dbo.AdminPermission (Name, Code, MenuPath, Sort, IsEnabled, CreatedAt)
SELECT p.Name, p.Code, p.MenuPath, p.Sort, 1, @Now
FROM @Permissions p
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.AdminPermission existing
    WHERE existing.Code = p.Code
);

UPDATE existing
SET
    existing.Name = p.Name,
    existing.MenuPath = p.MenuPath,
    existing.Sort = p.Sort,
    existing.IsEnabled = 1
FROM dbo.AdminPermission existing
JOIN @Permissions p ON p.Code = existing.Code;

INSERT INTO dbo.AdminRoleMenu (AdminRoleId, AdminMenuId, CreatedAt)
SELECT @SuperAdminRoleId, m.Id, @Now
FROM dbo.AdminMenu m
JOIN @Menus expected ON expected.Path = m.Path
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.AdminRoleMenu existing
    WHERE existing.AdminRoleId = @SuperAdminRoleId
      AND existing.AdminMenuId = m.Id
);

INSERT INTO dbo.AdminRolePermission (AdminRoleId, AdminPermissionId, CreatedAt)
SELECT @SuperAdminRoleId, p.Id, @Now
FROM dbo.AdminPermission p
JOIN @Permissions expected ON expected.Code = p.Code
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.AdminRolePermission existing
    WHERE existing.AdminRoleId = @SuperAdminRoleId
      AND existing.AdminPermissionId = p.Id
);

SELECT
    (SELECT COUNT(*) FROM dbo.AdminMenu) AS AdminMenuCount,
    (SELECT COUNT(*) FROM dbo.AdminPermission) AS AdminPermissionCount,
    (SELECT COUNT(*) FROM dbo.AdminRoleMenu WHERE AdminRoleId = @SuperAdminRoleId) AS SuperAdminMenuCount,
    (SELECT COUNT(*) FROM dbo.AdminRolePermission WHERE AdminRoleId = @SuperAdminRoleId) AS SuperAdminPermissionCount;

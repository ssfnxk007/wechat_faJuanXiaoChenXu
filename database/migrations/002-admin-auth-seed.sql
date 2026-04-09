IF NOT EXISTS (SELECT 1 FROM AdminUser WHERE Username = 'admin')
BEGIN
    INSERT INTO AdminUser (Username, PasswordHash, DisplayName, IsEnabled, CreatedAt)
    VALUES ('admin', '8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'系统管理员', 1, GETDATE())
END;

IF EXISTS (SELECT 1 FROM AdminUser WHERE Username = 'admin' AND ISNULL(PasswordHash, '') = '')
BEGIN
    UPDATE AdminUser
    SET PasswordHash = '8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92',
        DisplayName = CASE WHEN ISNULL(DisplayName, '') = '' THEN N'系统管理员' ELSE DisplayName END,
        IsEnabled = 1
    WHERE Username = 'admin'
END;

IF NOT EXISTS (SELECT 1 FROM AdminRole WHERE Code = 'super_admin')
BEGIN
    INSERT INTO AdminRole (Name, Code, IsEnabled, CreatedAt)
    VALUES (N'超级管理员', 'super_admin', 1, GETDATE())
END;

IF NOT EXISTS (
    SELECT 1
    FROM AdminUserRole ur
    INNER JOIN AdminUser u ON ur.AdminUserId = u.Id
    INNER JOIN AdminRole r ON ur.AdminRoleId = r.Id
    WHERE u.Username = 'admin' AND r.Code = 'super_admin'
)
BEGIN
    INSERT INTO AdminUserRole (AdminUserId, AdminRoleId, CreatedAt)
    SELECT u.Id, r.Id, GETDATE()
    FROM AdminUser u
    CROSS JOIN AdminRole r
    WHERE u.Username = 'admin' AND r.Code = 'super_admin'
END;

IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/users')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'用户管理', '/users', 'UserView', 10, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/stores')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'门店管理', '/stores', 'StoreView', 20, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/products')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'商品管理', '/products', 'ProductView', 30, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/coupon-templates')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'券模板管理', '/coupon-templates', 'CouponTemplateView', 40, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/coupon-packs')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'券包管理', '/coupon-packs', 'CouponPackView', 50, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/coupon-pack-items')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'券包明细', '/coupon-pack-items', 'CouponPackItemView', 60, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/coupon-orders')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'订单管理', '/coupon-orders', 'CouponOrderView', 70, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/user-coupons')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'用户券', '/user-coupons', 'UserCouponView', 80, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/writeoff')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'核销中心', '/writeoff', 'WriteOffView', 90, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/admin-users')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'权限管理', '/admin-users', 'AdminUserView', 100, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/admin-roles')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'角色管理', '/admin-roles', 'AdminRoleView', 110, 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE Path = '/admin-menus')
    INSERT INTO AdminMenu (ParentId, Name, Path, Component, Sort, IsEnabled, CreatedAt) VALUES (NULL, N'菜单管理', '/admin-menus', 'AdminMenuView', 120, 1, GETDATE());

INSERT INTO AdminRoleMenu (AdminRoleId, AdminMenuId, CreatedAt)
SELECT r.Id, m.Id, GETDATE()
FROM AdminRole r
CROSS JOIN AdminMenu m
WHERE r.Code = 'super_admin'
AND NOT EXISTS (
    SELECT 1 FROM AdminRoleMenu rm WHERE rm.AdminRoleId = r.Id AND rm.AdminMenuId = m.Id
);

IF OBJECT_ID(N'[dbo].[AdminPermission]', N'U') IS NULL
BEGIN
    RAISERROR(N'表 AdminPermission 不存在，请先执行 database/migrations/003-admin-permission.sql', 16, 1);
    RETURN;
END;
GO

IF OBJECT_ID(N'[dbo].[AdminRolePermission]', N'U') IS NULL
BEGIN
    RAISERROR(N'表 AdminRolePermission 不存在，请先执行 database/migrations/003-admin-permission.sql', 16, 1);
    RETURN;
END;
GO

DECLARE @permissions TABLE
(
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [MenuPath] nvarchar(100) NOT NULL,
    [Sort] int NOT NULL
);

INSERT INTO @permissions ([Name], [Code], [MenuPath], [Sort])
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

    (N'执行核销', 'writeoff.execute', '/writeoff', 10);

INSERT INTO [dbo].[AdminPermission] ([Name], [Code], [MenuPath], [Sort], [IsEnabled], [CreatedAt])
SELECT p.[Name], p.[Code], p.[MenuPath], p.[Sort], 1, GETDATE()
FROM @permissions p
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[AdminPermission] ap
    WHERE ap.[Code] = p.[Code]
);

UPDATE ap
SET ap.[Name] = p.[Name],
    ap.[MenuPath] = p.[MenuPath],
    ap.[Sort] = p.[Sort],
    ap.[IsEnabled] = 1
FROM [dbo].[AdminPermission] ap
INNER JOIN @permissions p ON p.[Code] = ap.[Code]
WHERE ap.[Name] <> p.[Name]
   OR ap.[MenuPath] <> p.[MenuPath]
   OR ap.[Sort] <> p.[Sort]
   OR ap.[IsEnabled] <> 1;

INSERT INTO [dbo].[AdminRolePermission] ([AdminRoleId], [AdminPermissionId], [CreatedAt])
SELECT r.[Id], p.[Id], GETDATE()
FROM [dbo].[AdminRole] r
CROSS JOIN [dbo].[AdminPermission] p
WHERE r.[Code] = 'super_admin'
  AND NOT EXISTS (
      SELECT 1
      FROM [dbo].[AdminRolePermission] rp
      WHERE rp.[AdminRoleId] = r.[Id]
        AND rp.[AdminPermissionId] = p.[Id]
  );
GO


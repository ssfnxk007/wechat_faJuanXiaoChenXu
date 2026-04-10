IF OBJECT_ID(N'[dbo].[AdminRole]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[AdminMenu]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[AdminRoleMenu]', N'U') IS NULL
BEGIN
    RAISERROR(N'缺少后台权限相关表，请先执行 002-admin-auth.sql', 16, 1);
    RETURN;
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AdminMenu] WHERE [Path] = N'/admin-users')
BEGIN
    INSERT INTO [dbo].[AdminMenu] ([ParentId], [Name], [Path], [Component], [Sort], [IsEnabled], [CreatedAt])
    VALUES (NULL, N'权限管理', N'/admin-users', N'AdminUserView', 100, 1, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AdminMenu] WHERE [Path] = N'/admin-roles')
BEGIN
    INSERT INTO [dbo].[AdminMenu] ([ParentId], [Name], [Path], [Component], [Sort], [IsEnabled], [CreatedAt])
    VALUES (NULL, N'角色管理', N'/admin-roles', N'AdminRoleView', 110, 1, GETDATE());
END;
GO

INSERT INTO [dbo].[AdminRoleMenu] ([AdminRoleId], [AdminMenuId], [CreatedAt])
SELECT r.[Id], m.[Id], GETDATE()
FROM [dbo].[AdminRole] r
INNER JOIN [dbo].[AdminMenu] m ON m.[Path] IN (N'/admin-users', N'/admin-roles')
WHERE r.[Code] = 'super_admin'
  AND NOT EXISTS (
      SELECT 1
      FROM [dbo].[AdminRoleMenu] rm
      WHERE rm.[AdminRoleId] = r.[Id]
        AND rm.[AdminMenuId] = m.[Id]
  );
GO

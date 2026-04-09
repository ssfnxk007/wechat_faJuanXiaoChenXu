BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[dbo].[AdminPermission]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AdminPermission] (
        [Id] bigint NOT NULL IDENTITY(1,1),
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [MenuPath] nvarchar(100) NOT NULL,
        [Sort] int NOT NULL,
        [IsEnabled] bit NOT NULL CONSTRAINT [DF_AdminPermission_IsEnabled] DEFAULT ((1)),
        [CreatedAt] datetime NOT NULL CONSTRAINT [DF_AdminPermission_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_AdminPermission] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'[dbo].[AdminRolePermission]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AdminRolePermission] (
        [Id] bigint NOT NULL IDENTITY(1,1),
        [AdminRoleId] bigint NOT NULL,
        [AdminPermissionId] bigint NOT NULL,
        [CreatedAt] datetime NOT NULL CONSTRAINT [DF_AdminRolePermission_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_AdminRolePermission] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AdminPermission_Code'
      AND object_id = OBJECT_ID(N'[dbo].[AdminPermission]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_AdminPermission_Code] ON [dbo].[AdminPermission] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AdminRolePermission_AdminRoleId_AdminPermissionId'
      AND object_id = OBJECT_ID(N'[dbo].[AdminRolePermission]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_AdminRolePermission_AdminRoleId_AdminPermissionId]
        ON [dbo].[AdminRolePermission] ([AdminRoleId], [AdminPermissionId]);
END;
GO

COMMIT;
GO

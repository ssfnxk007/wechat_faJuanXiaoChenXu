BEGIN TRANSACTION;
GO

CREATE TABLE [AdminPermission] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [MenuPath] nvarchar(100) NOT NULL,
    [Sort] int NOT NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_AdminPermission] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AdminRolePermission] (
    [Id] bigint NOT NULL IDENTITY,
    [AdminRoleId] bigint NOT NULL,
    [AdminPermissionId] bigint NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_AdminRolePermission] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_AdminPermission_Code] ON [AdminPermission] ([Code]);
GO

CREATE UNIQUE INDEX [IX_AdminRolePermission_AdminRoleId_AdminPermissionId] ON [AdminRolePermission] ([AdminRoleId], [AdminPermissionId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260410022408_AddAdminPermissionTables', N'8.0.8');
GO

COMMIT;
GO


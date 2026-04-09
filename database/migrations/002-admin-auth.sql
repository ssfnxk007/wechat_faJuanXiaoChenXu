BEGIN TRANSACTION;
GO

CREATE TABLE [AdminMenu] (
    [Id] bigint NOT NULL IDENTITY,
    [ParentId] bigint NULL,
    [Name] nvarchar(50) NOT NULL,
    [Path] nvarchar(100) NOT NULL,
    [Component] nvarchar(100) NOT NULL,
    [Sort] int NOT NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_AdminMenu] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AdminRole] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_AdminRole] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AdminRoleMenu] (
    [Id] bigint NOT NULL IDENTITY,
    [AdminRoleId] bigint NOT NULL,
    [AdminMenuId] bigint NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_AdminRoleMenu] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AdminUser] (
    [Id] bigint NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(128) NOT NULL,
    [DisplayName] nvarchar(50) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_AdminUser] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AdminUserRole] (
    [Id] bigint NOT NULL IDENTITY,
    [AdminUserId] bigint NOT NULL,
    [AdminRoleId] bigint NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_AdminUserRole] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_AdminRole_Code] ON [AdminRole] ([Code]);
GO

CREATE UNIQUE INDEX [IX_AdminRoleMenu_AdminRoleId_AdminMenuId] ON [AdminRoleMenu] ([AdminRoleId], [AdminMenuId]);
GO

CREATE UNIQUE INDEX [IX_AdminUser_Username] ON [AdminUser] ([Username]);
GO

CREATE UNIQUE INDEX [IX_AdminUserRole_AdminUserId_AdminRoleId] ON [AdminUserRole] ([AdminUserId], [AdminRoleId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260409052645_AddAdminAuthTables', N'8.0.8');
GO

COMMIT;
GO


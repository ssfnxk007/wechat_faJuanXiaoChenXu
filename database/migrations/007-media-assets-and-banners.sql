IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[CouponTemplate]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[CouponPack]', N'U') IS NULL
BEGIN
    RAISERROR(N'缺少商品或券相关表，请先执行基础建表脚本', 16, 1);
    RETURN;
END;
GO

IF OBJECT_ID(N'[dbo].[MediaAsset]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MediaAsset]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [FileUrl] nvarchar(500) NOT NULL,
        [MediaType] nvarchar(20) NOT NULL CONSTRAINT [DF_MediaAsset_MediaType] DEFAULT (N'image'),
        [BucketType] nvarchar(20) NOT NULL,
        [Tags] nvarchar(500) NULL,
        [Sort] int NOT NULL CONSTRAINT [DF_MediaAsset_Sort] DEFAULT ((0)),
        [IsEnabled] bit NOT NULL CONSTRAINT [DF_MediaAsset_IsEnabled] DEFAULT ((1)),
        [CreatedAt] datetime NOT NULL CONSTRAINT [DF_MediaAsset_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_MediaAsset] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaAsset_BucketType_IsEnabled_Sort_Id' AND object_id = OBJECT_ID(N'[dbo].[MediaAsset]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MediaAsset_BucketType_IsEnabled_Sort_Id]
        ON [dbo].[MediaAsset]([BucketType] ASC, [IsEnabled] ASC, [Sort] ASC, [Id] ASC);
END;
GO

IF OBJECT_ID(N'[dbo].[Banner]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Banner]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [ImageAssetId] bigint NOT NULL,
        [LinkUrl] nvarchar(500) NULL,
        [Sort] int NOT NULL CONSTRAINT [DF_Banner_Sort] DEFAULT ((0)),
        [IsEnabled] bit NOT NULL CONSTRAINT [DF_Banner_IsEnabled] DEFAULT ((1)),
        [CreatedAt] datetime NOT NULL CONSTRAINT [DF_Banner_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_Banner] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Banner_IsEnabled_Sort_Id' AND object_id = OBJECT_ID(N'[dbo].[Banner]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Banner_IsEnabled_Sort_Id]
        ON [dbo].[Banner]([IsEnabled] ASC, [Sort] ASC, [Id] ASC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Banner_MediaAsset_ImageAssetId')
BEGIN
    ALTER TABLE [dbo].[Banner] WITH CHECK
        ADD CONSTRAINT [FK_Banner_MediaAsset_ImageAssetId]
        FOREIGN KEY([ImageAssetId]) REFERENCES [dbo].[MediaAsset]([Id]);
END;
GO

IF COL_LENGTH(N'dbo.Product', N'MainImageAssetId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Product] ADD [MainImageAssetId] bigint NULL;
END;
GO

IF COL_LENGTH(N'dbo.Product', N'DetailImageAssetIds') IS NULL
BEGIN
    ALTER TABLE [dbo].[Product] ADD [DetailImageAssetIds] nvarchar(max) NULL;
END;
GO

IF COL_LENGTH(N'dbo.CouponTemplate', N'ImageAssetId') IS NULL
BEGIN
    ALTER TABLE [dbo].[CouponTemplate] ADD [ImageAssetId] bigint NULL;
END;
GO

IF COL_LENGTH(N'dbo.CouponPack', N'ImageAssetId') IS NULL
BEGIN
    ALTER TABLE [dbo].[CouponPack] ADD [ImageAssetId] bigint NULL;
END;
GO

IF COL_LENGTH(N'dbo.Product', N'MainImageUrl') IS NOT NULL
BEGIN
    PRINT N'保留旧字段 Product.MainImageUrl，待数据迁移完成后再手工清理。';
END;
GO

IF COL_LENGTH(N'dbo.Product', N'DetailImageUrls') IS NOT NULL
BEGIN
    PRINT N'保留旧字段 Product.DetailImageUrls，待数据迁移完成后再手工清理。';
END;
GO

IF COL_LENGTH(N'dbo.CouponTemplate', N'ImageUrl') IS NOT NULL
BEGIN
    PRINT N'保留旧字段 CouponTemplate.ImageUrl，待数据迁移完成后再手工清理。';
END;
GO

IF COL_LENGTH(N'dbo.CouponPack', N'ImageUrl') IS NOT NULL
BEGIN
    PRINT N'保留旧字段 CouponPack.ImageUrl，待数据迁移完成后再手工清理。';
END;
GO

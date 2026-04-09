IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AppUser] (
    [Id] bigint NOT NULL IDENTITY,
    [MiniOpenId] nvarchar(64) NOT NULL,
    [UnionId] nvarchar(64) NULL,
    [OfficialOpenId] nvarchar(64) NULL,
    [Mobile] nvarchar(20) NULL,
    [Nickname] nvarchar(100) NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_AppUser] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CouponOrder] (
    [Id] bigint NOT NULL IDENTITY,
    [OrderNo] nvarchar(50) NOT NULL,
    [AppUserId] bigint NOT NULL,
    [CouponPackId] bigint NOT NULL,
    [OrderAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [PaidAt] datetime NULL,
    [PaymentNo] nvarchar(64) NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_CouponOrder] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CouponPack] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [SalePrice] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [SaleStartTime] datetime NULL,
    [SaleEndTime] datetime NULL,
    [PerUserLimit] int NOT NULL,
    [Remark] nvarchar(500) NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_CouponPack] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CouponPackItem] (
    [Id] bigint NOT NULL IDENTITY,
    [CouponPackId] bigint NOT NULL,
    [CouponTemplateId] bigint NOT NULL,
    [Quantity] int NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_CouponPackItem] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CouponTemplate] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [TemplateType] int NOT NULL,
    [ValidPeriodType] int NOT NULL,
    [DiscountAmount] decimal(18,2) NULL,
    [ThresholdAmount] decimal(18,2) NULL,
    [ValidDays] int NULL,
    [ValidFrom] datetime NULL,
    [ValidTo] datetime NULL,
    [IsNewUserOnly] bit NOT NULL,
    [IsAllStores] bit NOT NULL,
    [PerUserLimit] int NOT NULL,
    [IsEnabled] bit NOT NULL,
    [Remark] nvarchar(500) NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_CouponTemplate] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CouponWriteOffRecord] (
    [Id] bigint NOT NULL IDENTITY,
    [UserCouponId] bigint NOT NULL,
    [CouponCode] nvarchar(50) NOT NULL,
    [StoreId] bigint NOT NULL,
    [OperatorName] nvarchar(50) NULL,
    [DeviceCode] nvarchar(50) NULL,
    [WriteOffAt] datetime NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_CouponWriteOffRecord] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PaymentTransaction] (
    [Id] bigint NOT NULL IDENTITY,
    [CouponOrderId] bigint NOT NULL,
    [PaymentNo] nvarchar(50) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [ChannelTradeNo] nvarchar(64) NULL,
    [RawCallback] nvarchar(max) NULL,
    [PaidAt] datetime NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_PaymentTransaction] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Product] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [ErpProductCode] nvarchar(64) NOT NULL,
    [SalePrice] decimal(18,2) NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_Product] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Store] (
    [Id] bigint NOT NULL IDENTITY,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [ContactName] nvarchar(50) NULL,
    [ContactPhone] nvarchar(20) NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_Store] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UserCoupon] (
    [Id] bigint NOT NULL IDENTITY,
    [AppUserId] bigint NOT NULL,
    [CouponTemplateId] bigint NOT NULL,
    [CouponOrderId] bigint NULL,
    [CouponCode] nvarchar(50) NOT NULL,
    [Status] int NOT NULL,
    [ReceivedAt] datetime NOT NULL,
    [EffectiveAt] datetime NOT NULL,
    [ExpireAt] datetime NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_UserCoupon] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_AppUser_MiniOpenId] ON [AppUser] ([MiniOpenId]);
GO

CREATE INDEX [IX_AppUser_Mobile] ON [AppUser] ([Mobile]);
GO

CREATE UNIQUE INDEX [IX_CouponOrder_OrderNo] ON [CouponOrder] ([OrderNo]);
GO

CREATE INDEX [IX_CouponPackItem_CouponPackId_CouponTemplateId] ON [CouponPackItem] ([CouponPackId], [CouponTemplateId]);
GO

CREATE UNIQUE INDEX [IX_PaymentTransaction_PaymentNo] ON [PaymentTransaction] ([PaymentNo]);
GO

CREATE UNIQUE INDEX [IX_Product_ErpProductCode] ON [Product] ([ErpProductCode]);
GO

CREATE UNIQUE INDEX [IX_Store_Code] ON [Store] ([Code]);
GO

CREATE UNIQUE INDEX [IX_UserCoupon_CouponCode] ON [UserCoupon] ([CouponCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260408175041_InitialCreate', N'8.0.8');
GO

COMMIT;
GO


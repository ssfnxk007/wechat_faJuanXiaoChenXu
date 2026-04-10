BEGIN TRANSACTION;
GO

CREATE TABLE [CouponTemplateProductScope] (
    [Id] bigint NOT NULL IDENTITY,
    [CouponTemplateId] bigint NOT NULL,
    [ProductId] bigint NOT NULL,
    [CreatedAt] datetime NOT NULL,
    CONSTRAINT [PK_CouponTemplateProductScope] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_CouponTemplateProductScope_CouponTemplateId_ProductId] ON [CouponTemplateProductScope] ([CouponTemplateId], [ProductId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260410030401_AddCouponTemplateProductScope', N'8.0.8');
GO

COMMIT;
GO


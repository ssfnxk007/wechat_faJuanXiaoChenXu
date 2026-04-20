-- =============================================================================
-- 008-high-priority-hardening.sql
--
-- 本次变更配合代码层的 5 项高优先级修复，共覆盖：
--   · UserCoupon.RowVersion 列（配合 EF 乐观并发，防止并发核销双写）
--   · 6 个关键外键列补非唯一索引（订单/券/支付热路径扫描 → 索引命中）
--
-- 执行前请备份；所有语句均为幂等（IF NOT EXISTS）。
-- =============================================================================

BEGIN TRANSACTION;
GO

-- 1) UserCoupon.RowVersion（rowversion 类型自动维护版本号）
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'RowVersion' AND Object_ID = OBJECT_ID(N'[UserCoupon]')
)
BEGIN
    ALTER TABLE [UserCoupon] ADD [RowVersion] rowversion NOT NULL;
END;
GO

-- 2) 关键外键索引
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserCoupon_AppUserId' AND object_id = OBJECT_ID(N'[UserCoupon]'))
    CREATE INDEX [IX_UserCoupon_AppUserId] ON [UserCoupon] ([AppUserId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserCoupon_CouponOrderId' AND object_id = OBJECT_ID(N'[UserCoupon]'))
    CREATE INDEX [IX_UserCoupon_CouponOrderId] ON [UserCoupon] ([CouponOrderId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserCoupon_CouponTemplateId' AND object_id = OBJECT_ID(N'[UserCoupon]'))
    CREATE INDEX [IX_UserCoupon_CouponTemplateId] ON [UserCoupon] ([CouponTemplateId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CouponOrder_AppUserId_CouponPackId' AND object_id = OBJECT_ID(N'[CouponOrder]'))
    CREATE INDEX [IX_CouponOrder_AppUserId_CouponPackId] ON [CouponOrder] ([AppUserId], [CouponPackId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransaction_CouponOrderId' AND object_id = OBJECT_ID(N'[PaymentTransaction]'))
    CREATE INDEX [IX_PaymentTransaction_CouponOrderId] ON [PaymentTransaction] ([CouponOrderId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CouponWriteOffRecord_UserCouponId' AND object_id = OBJECT_ID(N'[CouponWriteOffRecord]'))
    CREATE INDEX [IX_CouponWriteOffRecord_UserCouponId] ON [CouponWriteOffRecord] ([UserCouponId]);
GO

COMMIT;
GO

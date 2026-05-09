SET NAMES utf8mb4;
SET time_zone = '+08:00';

CREATE TABLE `AppUser` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `MiniOpenId` varchar(64) NOT NULL,
  `UnionId` varchar(64) NULL,
  `OfficialOpenId` varchar(64) NULL,
  `Mobile` varchar(20) NULL,
  `Nickname` varchar(100) NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AppUser_MiniOpenId` (`MiniOpenId`),
  KEY `IX_AppUser_Mobile` (`Mobile`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AdminUser` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) NOT NULL,
  `PasswordHash` varchar(128) NOT NULL,
  `DisplayName` varchar(50) NOT NULL,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AdminUser_Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AdminRole` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL,
  `Code` varchar(50) NOT NULL,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AdminRole_Code` (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AdminMenu` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ParentId` bigint NULL,
  `Name` varchar(50) NOT NULL,
  `Path` varchar(100) NOT NULL,
  `Component` varchar(100) NOT NULL,
  `Sort` int NOT NULL DEFAULT 0,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_AdminMenu_ParentId` (`ParentId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AdminUserRole` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AdminUserId` bigint NOT NULL,
  `AdminRoleId` bigint NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AdminUserRole_AdminUserId_AdminRoleId` (`AdminUserId`, `AdminRoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AdminRoleMenu` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AdminRoleId` bigint NOT NULL,
  `AdminMenuId` bigint NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AdminRoleMenu_AdminRoleId_AdminMenuId` (`AdminRoleId`, `AdminMenuId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AdminPermission` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL,
  `Code` varchar(100) NOT NULL,
  `MenuPath` varchar(100) NOT NULL,
  `Sort` int NOT NULL DEFAULT 0,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AdminPermission_Code` (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AdminRolePermission` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AdminRoleId` bigint NOT NULL,
  `AdminPermissionId` bigint NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AdminRolePermission_AdminRoleId_AdminPermissionId` (`AdminRoleId`, `AdminPermissionId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `MediaAsset` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL,
  `FileUrl` varchar(500) NOT NULL,
  `MediaType` varchar(20) NOT NULL DEFAULT 'image',
  `BucketType` varchar(20) NOT NULL,
  `Tags` varchar(500) NULL,
  `Sort` int NOT NULL DEFAULT 0,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_MediaAsset_BucketType_IsEnabled_Sort_Id` (`BucketType`, `IsEnabled`, `Sort`, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Banner` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Title` varchar(100) NOT NULL,
  `ImageAssetId` bigint NOT NULL,
  `LinkUrl` varchar(500) NULL,
  `Sort` int NOT NULL DEFAULT 0,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_Banner_IsEnabled_Sort_Id` (`IsEnabled`, `Sort`, `Id`),
  CONSTRAINT `FK_Banner_MediaAsset_ImageAssetId` FOREIGN KEY (`ImageAssetId`) REFERENCES `MediaAsset` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Store` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Code` varchar(50) NOT NULL,
  `Name` varchar(100) NOT NULL,
  `ContactName` varchar(50) NULL,
  `ContactPhone` varchar(20) NULL,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Store_Code` (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Product` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL,
  `ErpProductCode` varchar(64) NOT NULL,
  `ErpIsbnCode` varchar(64) NULL,
  `MainImageAssetId` bigint NULL,
  `DetailImageAssetIds` longtext NULL,
  `ErpOriginalPrice` decimal(18,2) NULL,
  `SalePrice` decimal(18,2) NULL,
  `StockQuantity` int NULL,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `ShowInMiniApp` tinyint(1) NULL,
  `DirectPurchaseCouponTemplateId` bigint NULL,
  `DirectPurchaseValidPeriodType` int NULL,
  `DirectPurchaseValidDays` int NULL,
  `DirectPurchaseValidFrom` datetime NULL,
  `DirectPurchaseValidTo` datetime NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Product_ErpProductCode` (`ErpProductCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponTemplate` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL,
  `ImageAssetId` bigint NULL,
  `TemplateType` int NOT NULL,
  `ValidPeriodType` int NOT NULL,
  `DiscountAmount` decimal(18,2) NULL,
  `ThresholdAmount` decimal(18,2) NULL,
  `ValidDays` int NULL,
  `ValidFrom` datetime NULL,
  `ValidTo` datetime NULL,
  `IsNewUserOnly` tinyint(1) NOT NULL,
  `IsAllStores` tinyint(1) NOT NULL DEFAULT 1,
  `PerUserLimit` int NOT NULL DEFAULT 1,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT 1,
  `DistributionMode` int NOT NULL DEFAULT 0,
  `SalePrice` decimal(18,2) NULL,
  `Remark` varchar(500) NULL,
  `IsSystemProductVoucher` tinyint(1) NOT NULL DEFAULT 0,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponTemplateProductScope` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CouponTemplateId` bigint NOT NULL,
  `ProductId` bigint NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_CouponTemplateProductScope_CouponTemplateId_ProductId` (`CouponTemplateId`, `ProductId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponTemplateStoreScope` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CouponTemplateId` bigint NOT NULL,
  `StoreId` bigint NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_CouponTemplateStoreScope_CouponTemplateId_StoreId` (`CouponTemplateId`, `StoreId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponPack` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL,
  `ImageAssetId` bigint NULL,
  `SalePrice` decimal(18,2) NOT NULL,
  `Status` int NOT NULL,
  `SaleStartTime` datetime NULL,
  `SaleEndTime` datetime NULL,
  `PerUserLimit` int NOT NULL DEFAULT 1,
  `Remark` varchar(500) NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponPackItem` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CouponPackId` bigint NOT NULL,
  `CouponTemplateId` bigint NOT NULL,
  `Quantity` int NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_CouponPackItem_CouponPackId_CouponTemplateId` (`CouponPackId`, `CouponTemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponOrder` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `OrderNo` varchar(50) NOT NULL,
  `AppUserId` bigint NOT NULL,
  `SourceType` int NOT NULL DEFAULT 2,
  `CouponPackId` bigint NULL,
  `CouponTemplateId` bigint NULL,
  `ProductId` bigint NULL,
  `ProductNameSnapshot` longtext NULL,
  `ProductErpProductCodeSnapshot` longtext NULL,
  `OrderAmount` decimal(18,2) NOT NULL,
  `Status` int NOT NULL,
  `PaidAt` datetime NULL,
  `PaymentNo` varchar(64) NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_CouponOrder_OrderNo` (`OrderNo`),
  KEY `IX_CouponOrder_AppUserId_CouponPackId` (`AppUserId`, `CouponPackId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `PaymentTransaction` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CouponOrderId` bigint NOT NULL,
  `PaymentNo` varchar(50) NOT NULL,
  `Amount` decimal(18,2) NOT NULL,
  `Status` int NOT NULL,
  `ChannelTradeNo` varchar(64) NULL,
  `RawCallback` longtext NULL,
  `PaidAt` datetime NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_PaymentTransaction_PaymentNo` (`PaymentNo`),
  KEY `IX_PaymentTransaction_CouponOrderId` (`CouponOrderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `UserCoupon` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AppUserId` bigint NOT NULL,
  `CouponTemplateId` bigint NOT NULL,
  `CouponOrderId` bigint NULL,
  `SourceType` int NOT NULL DEFAULT 2,
  `BoundProductId` bigint NULL,
  `BoundProductName` longtext NULL,
  `BoundErpProductCode` longtext NULL,
  `CouponCode` varchar(50) NOT NULL,
  `Status` int NOT NULL,
  `FulfillmentStatus` int NOT NULL DEFAULT 0,
  `ReceivedAt` datetime NOT NULL,
  `EffectiveAt` datetime NOT NULL,
  `ExpireAt` datetime NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `RowVersion` timestamp(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_UserCoupon_CouponCode` (`CouponCode`),
  KEY `IX_UserCoupon_AppUserId` (`AppUserId`),
  KEY `IX_UserCoupon_CouponOrderId` (`CouponOrderId`),
  KEY `IX_UserCoupon_CouponTemplateId` (`CouponTemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponWriteOffRecord` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserCouponId` bigint NOT NULL,
  `CouponCode` varchar(50) NOT NULL,
  `StoreId` bigint NOT NULL,
  `ProductId` bigint NULL,
  `OperatorName` varchar(50) NULL,
  `DeviceCode` varchar(50) NULL,
  `WriteOffAt` datetime NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_CouponWriteOffRecord_UserCouponId` (`UserCouponId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `MiniAppShareEvent` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `EventKey` varchar(100) NOT NULL,
  `EventType` varchar(16) NOT NULL,
  `ShareId` varchar(40) NOT NULL,
  `FromUserId` bigint NULL,
  `OpenUserId` bigint NULL,
  `VisitorKey` varchar(64) NULL,
  `TargetType` varchar(16) NOT NULL,
  `TargetKey` varchar(64) NOT NULL,
  `TargetId` bigint NULL,
  `PagePath` varchar(128) NOT NULL,
  `Scene` varchar(32) NULL,
  `QueryJson` varchar(1000) NULL,
  `ClientTime` datetime NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Ip` varchar(64) NULL,
  `UserAgent` varchar(256) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_MiniAppShareEvent_EventKey` (`EventKey`),
  KEY `IX_MiniAppShareEvent_EventType_CreatedAt` (`EventType`, `CreatedAt`),
  KEY `IX_MiniAppShareEvent_FromUserId_CreatedAt` (`FromUserId`, `CreatedAt`),
  KEY `IX_MiniAppShareEvent_OpenUserId_CreatedAt` (`OpenUserId`, `CreatedAt`),
  KEY `IX_MiniAppShareEvent_ShareId` (`ShareId`),
  KEY `IX_MiniAppShareEvent_TargetType_TargetKey_CreatedAt` (`TargetType`, `TargetKey`, `CreatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponIssueImportBatch` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NULL,
  `FileName` varchar(200) NULL,
  `TotalCount` int NOT NULL DEFAULT 0,
  `MatchedCount` int NOT NULL DEFAULT 0,
  `FailedCount` int NOT NULL DEFAULT 0,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `CouponIssueImportDetail` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BatchId` bigint NOT NULL,
  `Mobile` varchar(20) NULL,
  `MiniOpenId` varchar(64) NULL,
  `OfficialOpenId` varchar(64) NULL,
  `CouponTemplateId` bigint NOT NULL,
  `Quantity` int NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_CouponIssueImportDetail_BatchId` (`BatchId`),
  KEY `IX_CouponIssueImportDetail_Mobile_Status` (`Mobile`, `Status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `WeChatPaySetting` (
  `Id` int NOT NULL,
  `AppId` varchar(64) NOT NULL,
  `MerchantId` varchar(32) NOT NULL,
  `MerchantSerialNo` varchar(128) NOT NULL,
  `PrivateKeyPem` longtext NOT NULL,
  `ApiV3Key` varchar(128) NOT NULL,
  `NotifyUrl` varchar(512) NOT NULL,
  `EnableMockFallback` tinyint(1) NOT NULL,
  `UpdatedAt` datetime NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

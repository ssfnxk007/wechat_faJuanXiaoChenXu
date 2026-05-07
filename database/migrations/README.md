# 数据库迁移说明

## 已生成内容

- EF 首个迁移：`backend/src/FaJuan.Api/Infrastructure/Persistence/Migrations/20260408175041_InitialCreate.cs`
- EF 后台权限迁移：`backend/src/FaJuan.Api/Infrastructure/Persistence/Migrations/20260409052645_AddAdminAuthTables.cs`
- 数据库模型快照：`backend/src/FaJuan.Api/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- 初版业务建表 SQL：`database/migrations/001-initial.sql`
- 后台权限表 SQL：`database/migrations/002-admin-auth.sql`
- 后台权限初始化 SQL：`database/migrations/002-admin-auth-seed.sql`
- 按钮权限表 SQL：`database/migrations/003-admin-permission.sql`
- 按钮权限初始化 SQL：`database/migrations/003-admin-permission-seed.sql`
- 按钮权限 EF 迁移脚本：`database/migrations/004-ef-admin-permission.sql`
- 券模板商品范围 EF 迁移脚本：`database/migrations/005-coupon-template-product-scope.sql`
- 高优先级安全与性能加固：`database/migrations/008-high-priority-hardening.sql`（UserCoupon 乐观并发列 + 6 个关键索引）

## 建议执行顺序

1. 确认 SQL Server 2008 R2 数据库已创建，例如：`FaJuanDb`
2. 先检查连接字符串与部署环境是否一致
3. 在测试环境按顺序执行：
   - `database/migrations/001-initial.sql`
   - `database/migrations/002-admin-auth.sql`
   - `database/migrations/002-admin-auth-seed.sql`
   - `database/migrations/003-admin-permission.sql`
   - `database/migrations/003-admin-permission-seed.sql`
   - `database/migrations/004-ef-admin-permission.sql`（如需按 EF 迁移历史收口）
   - `database/migrations/005-coupon-template-product-scope.sql`
4. 验证核心表是否创建成功：
   - `AppUser`
   - `Store`
   - `Product`
   - `CouponTemplate`
   - `CouponPack`
   - `CouponPackItem`
   - `CouponOrder`
   - `UserCoupon`
   - `PaymentTransaction`
   - `CouponWriteOffRecord`
5. 额外验证后台权限表是否创建成功：
   - `AdminUser`
   - `AdminRole`
   - `AdminMenu`
   - `AdminUserRole`
   - `AdminRoleMenu`
   - `AdminPermission`
   - `AdminRolePermission`
6. 后续每次实体变更后，重新生成迁移与 SQL
7. 若需要“指定商品券”完整核销校验，必须先执行 `005-coupon-template-product-scope.sql`

## 常用命令

- 生成迁移：
  - `dotnet dotnet-ef migrations add <MigrationName> --project backend/src/FaJuan.Api/FaJuan.Api.csproj --startup-project backend/src/FaJuan.Api/FaJuan.Api.csproj --output-dir Infrastructure/Persistence/Migrations`
- 导出 SQL：
  - `dotnet dotnet-ef migrations script --project backend/src/FaJuan.Api/FaJuan.Api.csproj --startup-project backend/src/FaJuan.Api/FaJuan.Api.csproj -o database/migrations/<file>.sql`

## 注意事项

- 后台管理员、角色、菜单、按钮权限现已全部落数据库表
- `002-admin-auth-seed.sql` 会初始化 `admin` 管理员、`super_admin` 角色及菜单授权
- `003-admin-permission-seed.sql` 会初始化按钮权限点，并将全部权限点授权给 `super_admin`
- `005-coupon-template-product-scope.sql` 会新增 `CouponTemplateProductScope` 表，用于指定商品券的适用商品范围校验
- 执行生产脚本前，建议先在测试库完整验证登录、创建订单、模拟支付、发券、核销主链路

## 迁移与实体时间字段约定（2026-04-20 起）

详见 `Docs/specs/2026-04-20-迁移与实体时间字段约定.md`。要点：

- 新迁移 DDL 时间列统一使用 `datetime2(3)`，不再使用 `datetime`（范围 1753-9999、3.3ms 精度不足）
- 新实体默认携带 `CreatedAt` + `UpdatedAt` 两个时间列，由 EF `SaveChangesInterceptor` 统一维护
- 历史表的 `datetime` 列暂不统一迁移，避免锁表；如需收口，单独起子计划（对应审核项 #22）
- 迁移文件命名规则保持：`NNN-<topic>.sql`，NNN 全局单调递增

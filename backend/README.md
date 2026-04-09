# backend

## 项目说明

- `src/FaJuan.Api`：主 API 项目
- `tests/FaJuan.Api.Tests`：基础测试项目

## 当前内容

- 统一 `ApiResponse<T>` 返回结构
- `HealthController` 健康检查接口：`GET /api/health`
- `AppDbContext` 与核心业务实体
- 后台 JWT 登录：`POST /api/adminauth/login`
- 小程序真实登录：`POST /api/auth/mini-login`
- SQL Server 连接字符串与微信小程序配置占位

## 默认后台账号

- 用户名：`admin`
- 密码：`123456`

建议在正式环境通过 `appsettings` 或环境变量覆盖。

## 微信小程序配置

在 `backend/src/FaJuan.Api/appsettings.json` 中填写：

- `WeChatMiniProgram:AppId`
- `WeChatMiniProgram:AppSecret`

填写后，后端会通过微信官方 `code2Session` 接口完成小程序登录。

## 命令

- 还原：`dotnet restore backend/FaJuan.sln`
- 构建：`dotnet build backend/FaJuan.sln -c Release -m:1`
- 测试：`dotnet test backend/tests/FaJuan.Api.Tests/FaJuan.Api.Tests.csproj -c Release`
- 新建迁移：`dotnet dotnet-ef migrations add InitialCreate --project backend/src/FaJuan.Api/FaJuan.Api.csproj --startup-project backend/src/FaJuan.Api/FaJuan.Api.csproj --output-dir Infrastructure/Persistence/Migrations`
- 导出 SQL：`dotnet dotnet-ef migrations script --project backend/src/FaJuan.Api/FaJuan.Api.csproj --startup-project backend/src/FaJuan.Api/FaJuan.Api.csproj -o database/migrations/001-initial.sql`

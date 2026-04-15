# Error.md

## 2026-04-10 - PowerShell 写文件乱码
- 症状：Markdown、SQL、Vue、TypeScript 等文件在 PowerShell 改写后出现中文乱码、引号断裂、构建报错。
- 根因：PowerShell 写文件时使用了不一致编码，或在带中文内容的文件上进行替换时没有复查输出结果。
- 修复：统一改为 plain `UTF-8` 写入；写入后立即复查文件关键片段；若已污染则重新以 `UTF-8` 重写。
- 预防：仓库内所有 PowerShell 文件写入只允许 `UTF-8`，禁止 BOM/UTF-16/ANSI/默认编码；中文文件改写后必须抽查。

## 2026-04-10 - 计划文档未同步完成状态
- 症状：实施计划仍是初始清单，实际开发进度没有 checkbox 勾选，用户无法直观看到完成情况。
- 根因：开发推进时只更新了工作日志，没有同步维护计划文档。
- 修复：后续每完成一个阶段，必须同步更新计划文档中的完成状态。
- 预防：将“计划文档同步”视为每轮交付的收尾动作之一，与工作日志同级处理。

## 2026-04-10 - 长任务无进度反馈被误判卡死
- 症状：用户在 30~40 分钟无消息后认为代理已卡死。
- 根因：长任务期间没有主动汇报当前进展、阻塞点与下一步。
- 修复：非简单任务使用多 Agent 并行，并在关键阶段主动反馈进度。
- 预防：长任务默认开启阶段性进度汇报；用户明确要求多 Agent 时必须持续执行。

## 2026-04-10 - PowerShell 直写不适合常规编辑
- 症状：即使指定了 UTF-8，PowerShell 直接改写中文文件时仍更容易引入乱码、引号断裂或局部替换失真。
- 根因：PowerShell 文本替换对复杂文件和中文内容不够稳，且回写范围不够可控。
- 修复：后续常规文件编辑统一优先使用 `apply_patch`，只在没有更安全手段时才考虑 PowerShell 直写。
- 预防：把 `apply_patch` 作为默认编辑方式，把 PowerShell 仅保留给读取、构建、查询、执行命令。

## 2026-04-10 - PowerShell 输出与终端显示乱码判定
- 症状：`Get-Content`、控制台输出或终端字体/代码页导致中文显示异常，看起来像文件已乱码，但实际文件可能仍是正确的 UTF-8。
- 根因：终端显示链路与文件存储编码不是一回事；PowerShell 控制台、宿主终端、字体和代码页都可能让 UTF-8 文件“看起来”像坏了。
- 修复：优先用文件字节、明确的 `UTF-8` 解码结果、编辑器打开效果，以及实际构建/运行结果来判断文件是否损坏；不要只根据终端显示下结论。
- 预防：遇到中文显示异常时，先做三步判定：1）检查文件是否能按 `UTF-8` 解码；2）在编辑器中复查关键片段；3）跑一次相关构建或最小验证。若文件内容正确且构建通过，可判定为“终端显示问题”而非“文件编码损坏”。

## 2026-04-10 - EF Core `Contains` 在旧 SQL Server 触发 `OPENJSON ... WITH` 语法错误
- 症状：后台登录后请求资料接口时报 `关键字 'WITH' 附近有语法错误`，SQL 中出现 `OPENJSON(@__roleIds_0) WITH ([value] bigint '$')`。
- 根因：EF Core 在 SQL Server 上把 `roleIds.Contains(x.AdminRoleId)` 翻译成 `OPENJSON` 方案，但当前数据库版本或兼容级别不支持该语法组合。
- 修复：避免在关键授权查询里对内存数组做 `Contains` 过滤，改为直接通过 `AdminUserRoles` 与 `AdminRoleMenus` / `AdminRolePermissions` 联表查询。
- 预防：本项目面向旧版 SQL Server / 低兼容级别时，优先使用显式联表、子查询或数据库原生可兼容写法；不要默认依赖 EF 对集合参数的 SQL Server 专有翻译结果。

## 2026-04-10 - EF Core `Skip/Take` 在旧 SQL Server 触发 `OFFSET/FETCH` 语法错误
- 症状：多个分页列表接口报 `OFFSET` / `FETCH NEXT` 语法错误，例如 `/api/users`、`/api/products`、`/api/stores`、`/api/coupontemplates`。
- 根因：EF Core 默认把 `OrderBy(...).Skip(...).Take(...)` 翻译成 `OFFSET ... FETCH`，而当前数据库环境接近 `SQL Server 2008 R2`，不支持该分页语法。
- 修复：新增 `QueryablePagingExtensions.ApplyLegacyPaging`，统一改成嵌套 `TOP` 的旧版 SQL Server 兼容分页写法。
- 预防：本项目所有后台分页查询都不要直接依赖 `Skip/Take` 生成 SQL；新增列表接口时优先复用统一的兼容分页扩展。

## 2026-04-10 - 权限按钮已下发但菜单未补齐会导致接口持续 403
- 症状：`/api/adminusers`、`/api/adminroles` 在管理员已具备 `super_admin` 角色和按钮权限时仍返回 `403`。
- 根因：数据库里缺少 `/admin-users`、`/admin-roles` 对应的 `AdminMenu` 记录与 `AdminRoleMenu` 绑定，菜单级鉴权先于按钮权限生效。
- 修复：补充幂等脚本 `database/migrations/006-admin-management-menu-backfill.sql`，同时回填菜单记录和 `super_admin` 菜单授权。
- 预防：新增后台模块时必须同时交付菜单数据、角色菜单绑定、按钮权限和角色按钮权限，不能只补其中一半。

## 2026-04-10 - EF 迁移 SQL 必须复查 SQL Server 2008 R2 兼容性
- 症状：EF Core 导出的 SQL 在高版本 SQL Server 可执行，但部署到 `SQL Server 2008 R2` 时可能因语法或 DDL 差异失败。
- 根因：EF 导出脚本不会自动替项目规避所有旧版数据库限制，而本项目目标数据库明确是 `SQL Server 2008 R2`。
- 修复：所有迁移 SQL 在交付前都要人工复查 `SQL Server 2008 R2` 兼容性，重点检查建表、索引、默认约束、批处理、系统表写入，以及是否混入高版本专属语法。
- 预防：以后凡是新增 `database/migrations/*.sql`，必须把“2008 R2 兼容性复查”作为交付步骤之一，并把结论写入工作日志或迁移说明。

## 2026-04-10 - 大型 `apply_patch` 在 Windows 下可能超出命令长度
- 症状：一次性提交超长补丁时失败，报 `文件名或扩展名太长`，看起来像路径问题，实际是命令行参数总长度超限。
- 根因：Windows 命令行长度有限，超大补丁通过单次 `apply_patch` 传参时会触发系统限制。
- 修复：把大文件改动拆成多个较小的 `apply_patch` 分段提交，避免退回到 PowerShell 直写。
- 预防：大页面、大文档、大 SQL 的新增或重写，优先按“骨架 / 逻辑 / 样式”分段补丁提交。

## 2026-04-10 - 分段补丁时要防止正则或字符串被意外断行
- 症状：Vue / TypeScript 文件构建失败，排查后发现 `.split(/[\s,]+/)` 之类的正则在分段补丁后被断成了跨行文本。
- 根因：手工拆分长补丁时，没有复查跨行附近的代码，导致正则、字符串或标签属性被切断。
- 修复：修正断裂的正则与属性，并在分段补丁后立即做 `rg` 检查和一次真实构建验证。
- 预防：凡是分段补丁涉及模板属性、正则、字符串、泛型、长表达式，都要在提交后进行一次文本巡检和构建验证。

## 2026-04-10 - 外部模板型 AGENTS 规则可能引用当前仓库不存在的文档
- 症状：按 `AGENTS.md` 指引读取 `Docs/OpenViking/...` 等文件时直接报文件不存在，容易误以为仓库缺损。
- 根因：当前工作目录复用了另一项目的模板化 `AGENTS.md` 内容，其中包含不属于本仓库的路径要求。
- 修复：改为以当前仓库实际文件为准，用全局检索确认现有文档、代码和迁移脚本后再继续实现。
- 预防：遇到模板式协作说明时，先验证路径是否真实存在；若不存在，应立即切换到仓库实际结构，不要围绕虚假路径做假设性开发。

## 2026-04-10 - 图片能力升级为素材库后不要继续沿用简单 URL 字段方案
- 症状：商品、券模板、券包一开始只新增了 `ImageUrl` / `MainImageUrl` 字段，但后续需求升级为商品图片区、轮播图区和素材复用区分开管理，简单字段方案无法支撑复用与分类。
- 根因：需求早期只按“给业务表补图片字段”理解，后续真实目标变成了素材中心模式。
- 修复：新增 `MediaAsset` 和 `Banner` 结构，并让商品、券模板、券包改为引用素材 ID；旧 URL 字段暂保留在数据库中用于过渡迁移。
- 预防：当需求包含“上传区分开管理”“素材复用”“轮播配置”时，默认优先考虑独立素材表，而不是继续向多个业务表散落 URL 字段。

## 2026-04-10 - 并行 agent 正在改 Vue 路由页时立即构建，可能出现 `Could not load ... .vue`
- 症状：`vite build` / `vue-tsc` 突然报 `Could not load src/views/.../*.vue`，像是文件丢失，但稍后文件又恢复或能从 `git` 找回。
- 根因：并行 agent 在重写大页面时会经历“先删旧文件再落新文件”的短暂窗口；若主线程恰好在这个窗口触发构建，打包器会把它识别成路由依赖丢失。
- 修复：先停止等待中的并行页面重写，恢复被临时删除的页面文件，再重新执行构建验证。
- 预防：多 agent 并行改前端大页面时，避免在它们尚未收口前立刻跑全量构建；优先让 agent 回报完成，再统一构建和合并。

## 2026-04-10 - ???????????????????????????? DTO
- ???? `SaveCouponTemplateRequest` / `SaveCouponPackRequest` ??????????Vue ???????????? `form.imageUrl` ???? TypeScript ????????
- ???????????????????????? UI ???????????????????? DTO????????????????????
- ??????? DTO ??????????????????????????? `SaveRequest & { imageUrl?: string }`???????? `buildPayload()` ????????
- ?????????????? / ??????????? DTO ?????????????????????????????????????????????????????????????

## 2026-04-10 - `powershell.exe 5.1` 可能因编码链路不统一导致中文乱码
- 症状：终端里看似已经在使用 UTF-8，但执行命令、管道传递文本、脚本写文件后，中文仍然出现乱码、`???`、断裂或被错误替换。
- 根因：这台机器实际存在 `powershell.exe 5.1` 会话默认编码链路不统一的问题，曾出现 `[Console]::InputEncoding = gb2312`、`[Console]::OutputEncoding = utf-8`、`$OutputEncoding = us-ascii`` 的混合状态；即使安装了 PowerShell 7 或 Windows Terminal，也不会自动修复正在运行的旧 PowerShell 会话。
- 修复：为 `WindowsPowerShell` 和 `PowerShell 7` 都写入 UTF-8 profile，引导新会话统一 `[Console]::InputEncoding`、`[Console]::OutputEncoding`、`$OutputEncoding`，并把 `Out-File`、`Set-Content`、`Add-Content` 默认编码固定为 `utf-8`；同时在项目协作中把中文敏感文件编辑优先级改为 `apply_patch` / `python` / `pwsh`。
- 预防：以后不要假设“装了 PowerShell 7 就没事”；必须先验证当前 shell 实际版本和三项编码状态。对中文代码、文档、SQL、配置文件，避免使用 `powershell.exe 5.1` 直接改写；不要尝试通过卸载 `PowerShell 5.1` 来解决此问题。

## 2026-04-12 - 独立菜单权限 SQL 与控制器鉴权未同步会导致“看得到菜单但点不开接口”
- 症状：数据库已经补了新的后台菜单路径和按钮权限，例如 `/banners` 与 `banner.*`，但实际接口仍然返回 `403`，或者页面进入后按钮显示与接口鉴权不一致。
- 根因：控制器上的 `[AdminMenuAuthorize]` 与 `[AdminPermissionAuthorize]` 仍沿用旧的菜单路径或旧权限码，导致初始化 SQL 与运行时鉴权规则脱节。
- 修复：在补菜单/权限初始化 SQL 时，同时核对控制器上的菜单路径、按钮权限码与前端路由；若本轮只允许改 SQL/文档，必须在工作日志和脚本注释中明确“当前运行时仍依赖旧权限”。
- 预防：新增后台模块时，菜单路径、按钮权限码、前端路由、初始化 SQL 必须四项一起核对；不要只补数据库基础数据而不检查控制器特性标记。


## 2026-04-11 - EF Core 分页生成 OFFSET/FETCH 导致 SQL Server 2008 R2 报错
- 症状：小程序接口调用时报错 `OFFSET 附近有语法错误`、`FETCH NEXT 用法无效`，堆栈落在 `MiniAppController.GetUserCoupons` / `GetUserOrders`。
- 根因：虽然项目已有 `ApplyLegacyPaging` 兼容扩展，但个别查询仍直接使用了 LINQ `Skip(...).Take(...)`，EF Core 对 SQL Server 生成了 `OFFSET/FETCH` 语句；SQL Server 2008 R2 不支持该语法。
- 修复：将残留的数据库分页查询统一改为 `ApplyLegacyPaging(pageIndex, pageSize, x => x.Id)`，避免生成高版本分页 SQL。
- 预防规则：本项目所有面向数据库的分页查询都禁止直接写 `Skip/Take`；涉及 SQL Server 2008 R2 的分页一律复用 `backend/src/FaJuan.Api/Application/Common/QueryablePagingExtensions.cs`。

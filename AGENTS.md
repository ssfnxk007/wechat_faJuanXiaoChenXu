# AGENTS.md

## Purpose
- This file guides coding agents working in `C:\Users\ssfnxk\Desktop\后台管理框架`.
- Follow repository conventions before introducing new patterns.
- Prefer modifying existing flows over inventing parallel implementations.

## Repo Layout
- `backend/`: .NET 8 solution `AdminSystem.sln`
- `frontend/`: Vue 3 + TypeScript + Vite admin UI
- `pos-client/`: Electron + Vue + TypeScript POS client
- `Docs/`: work logs, SQL scripts, architecture docs, OpenViking memory
- `Docs/OpenViking/`: short-form reusable project memory and pitfalls

## Required Reading Before Editing
- Read relevant work logs first when continuing active business work:
  - `Docs/2026-03-23-工作.md`
  - `Docs/2026-03-24-工作.md`
  - `Docs/2026-03-25-工作.md`
- For UI, retail, session, report, print, or auth tasks, read matching OpenViking notes first.
- Start with these high-value files:
  - `Docs/OpenViking/README.md`
  - `Docs/OpenViking/10-business-rules/ui-generation-baseline.md`
  - `Docs/OpenViking/10-business-rules/retail-module-baseline.md`
  - `Docs/OpenViking/20-api-contracts/main-detail-api-contract.md`
  - `Docs/OpenViking/20-api-contracts/auth-and-session-contract.md`
  - `Docs/OpenViking/30-known-issues/ui-generation-pitfalls.md`

## External Agent Rules
- No `.cursor/rules/`, `.cursorrules`, or `.github/copilot-instructions.md` were found at the time this file was written.
- If those files are added later, merge their requirements into your plan before coding.

## Hard Project Rules
- If a task requires creating tables, altering schema, adding columns, changing indexes, or changing foreign keys, stop and ask the user to confirm first.
- When asking for schema approval, explain why the DB change is needed, what it affects, and which SQL must be executed.
- New backend menus/pages must ship with executable SQL for menu initialization, permission binding, and dict data when applicable.
- Runtime global config must use `db_network_profile_ext`; do not add runtime fallback to `db_network_profile`.
- Store daily report logic must stay tied to `PH`/cashier sessions, not as a detached standalone workflow.

## Build Commands

### Backend
- Build solution:
  - `dotnet build "backend/AdminSystem.sln" -c Release -m:1`
- Build WebAPI only:
  - `dotnet build "backend/src/AdminSystem.WebAPI/AdminSystem.WebAPI.csproj" -c Release -m:1`
- If output DLLs are locked by a running API, either stop the process or build to a temp output folder.
- Known pattern from prior work:
  - `dotnet build .\src\AdminSystem.WebAPI\AdminSystem.WebAPI.csproj -nologo -o %TEMP%\AdminSystem.WebAPI-build /p:UseAppHost=false`

### Frontend
- Install deps: `npm install`
- Dev server: `npm run dev`
- Production build: `npm run build`

### POS Client
- Install deps: `npm install`
- Dev app: `npm run dev`
- Typecheck only: `npm run typecheck`
- Lint: `npm run lint`
- Format: `npm run format`
- Production build: `npm run build`
- Platform package builds:
  - `npm run build:win`
  - `npm run build:mac`
  - `npm run build:linux`

## Test Commands

### Backend Tests
- Run all tests:
  - `dotnet test "backend/tests/AdminSystem.WebAPI.Tests/AdminSystem.WebAPI.Tests.csproj" -c Release`
- Run a single test by fully-qualified name:
  - `dotnet test "backend/tests/AdminSystem.WebAPI.Tests/AdminSystem.WebAPI.Tests.csproj" -c Release --filter "FullyQualifiedName~Namespace.ClassName.TestName"`
- Run all tests in one class:
  - `dotnet test "backend/tests/AdminSystem.WebAPI.Tests/AdminSystem.WebAPI.Tests.csproj" -c Release --filter "FullyQualifiedName~Namespace.ClassName"`
- If the test project is already built:
  - `dotnet test "backend/tests/AdminSystem.WebAPI.Tests/AdminSystem.WebAPI.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~..."`

### Frontend / POS Tests
- No dedicated unit test suites were found in `frontend/` or `pos-client/`.
- Use build + typecheck + targeted manual verification as the default validation strategy.

## Lint / Validation Strategy
- `frontend/` has no standalone lint script; validate it with `npm run build` and targeted file review.
- `pos-client/` should usually be validated with:
  - `npm run lint`
  - `npm run typecheck`
  - `npm run build`
- For cross-stack changes touching API contracts, run all three builds:
  - backend WebAPI build
  - frontend build
  - pos-client build

## Code Style: General
- Keep changes small and consistent with nearby files.
- Prefer ASCII for edits unless the file already contains Chinese UI text or SQL messages.
- Preserve existing Chinese business wording where the UI already uses it.
- Avoid broad rewrites when a focused fix is sufficient.
- Do not add comments for obvious code; add them only for non-obvious business rules or compatibility hacks.

## Code Style: Imports
- C#: keep `using` directives grouped at the top; alias only when there is a real collision.
- TypeScript/Vue: keep framework imports first, then third-party libs, then local `@/` or relative imports.
- Reuse existing path aliases such as `@/api`, `@/stores`, `@/components` in admin frontend.

## Code Style: Formatting
- `pos-client/.editorconfig` is authoritative where applicable:
  - UTF-8
  - spaces
  - indent size 2
  - LF
  - final newline
  - trim trailing whitespace
- Match surrounding formatting in backend C# files, which currently use 4-space indentation.
- Vue SFCs typically use `<script setup lang="ts">`.

## Code Style: TypeScript / Vue
- Use explicit interfaces/types for API data structures.
- Keep API wrappers thin; business shaping belongs in views/composables or backend DTOs.
- Prefer `ref`/`reactive` with narrow, named types.
- Do not swallow errors silently; log or surface actionable context unless user cancellation is expected.
- Avoid introducing new UI skeletons when an existing page pattern already exists.
- For admin pages, prefer existing `search-card + data-card` patterns.
- For main-detail flows, prefer dialog-based detail interaction instead of inline ad hoc layouts.

## Code Style: C# / Backend
- Use constructor injection and keep dependencies explicit.
- Return unified `ApiResponse<T>` shapes from controllers.
- Guard invalid input early and return clear failure messages.
- Prefer structured DTOs over loose `whereSql`-style filters unless the module already depends on them.
- Reuse existing helpers/services before introducing duplicate utility methods.
- Keep SQL parameterized via `SugarParameter`; never interpolate raw user input into SQL.

## Naming Conventions
- Follow existing domain names rather than renaming for style.
- Backend DTOs and controllers use PascalCase members and type names.
- Frontend and POS variables/functions use camelCase.
- Vue component filenames are generally PascalCase; route/view folders often use kebab-case or module folders.
- Keep API field names aligned exactly with backend contracts.

## Error Handling
- Do not use empty `catch {}` blocks.
- For recoverable UI-side failures, prefer `console.warn('[context]', err)` or `ElMessage` when user-facing.
- For auth failures in admin frontend, preserve unified interceptor behavior in `frontend/src/api/index.ts`.
- For backend transaction flows, rollback defensively and return contextual failure messages.

## API Contract Rules
- Admin frontend expects unified backend responses shaped like `ApiResponse<T>` and unwraps `data` when `code === 200`.
- Paged lists should follow `PagedResponse<T>` with `items`, `totalCount`, `pageIndex`, `pageSize`, `totalPages`.
- Query forms must map 1:1 to backend DTO/query parameters.
- Do not change payload shape casually when a module already has a live contract.

## UI / UX Rules
- First classify the page: basic CRUD, main-detail, print, retail settings, cashier/session workflow.
- Reuse existing project patterns before creating new ones.
- Keep admin UI dense, practical, and business-first.
- For printable reports, preserve line-of-business layout fidelity over generic dashboard aesthetics.
- After editing Chinese UI text, visually recheck for encoding corruption like `?`, `??`, or garbled text.

## Retail / POS Rules
- Retail settings have strict global vs station boundaries; verify before changing storage or cache keys.
- Sensitive payment config fields require masking and old-value retention rules.
- POS station settings often prefer station-specific cache keys such as `pos_settings_{stationId}`.
- Cashier/session/report workflows are high-regression areas; validate with real build flows after changes.

## SQL / Migration Rules
- Put executable SQL in `Docs/sql/` or the task-specific SQL file already used by the feature.
- Favor idempotent SQL when possible.
- Preserve legacy compatibility where current scripts already follow that pattern.
- When changing report/session logic, document execution order and rollback impact in task notes.
- All EF-generated migration SQL must be reviewed for `SQL Server 2008 R2` compatibility before hand-off.
- Do not assume newer SQL Server syntax is available; avoid emitting unsupported features without fallback or manual rewrite.
- When exporting EF migration scripts for this project, explicitly verify statements, index syntax, default constraints, and DDL are compatible with `SQL Server 2008 R2`.

## Practical Workflow For Agents
- Read relevant OpenViking notes before editing.
- Inspect existing implementation near the target file and copy local patterns.
- Make the smallest coherent change.
- Validate the touched stack with real build/test commands.
- If you discover a reusable rule or regression hotspot, update `Docs/OpenViking/` after the task.

## Multi-Agent Collaboration Rule
- For any non-trivial task that spans multiple files, modules, or stacks, prefer using multiple agents in parallel.
- Default expectation: split backend, frontend, documentation, verification, or code exploration into separate agents when scopes are independent.
- The main agent should still own the critical path, integrate results, and avoid duplicate work across agents.
- If a task runs for a long time, provide progress updates proactively instead of waiting silently.
- If there is no visible progress for a while, report current status, what is blocked, and the next action.
- When a user explicitly asks for multi-agent execution, treat it as a high-priority workflow requirement for the rest of the task unless the user changes it.

## Work Log Rules
- After any task that results in actual code, SQL, config, or documentation changes, update the current day's work log automatically.
- Work log filename format: `Docs/YYYY-MM-DD-工作.md`.
- If the current day's file does not exist, create it. If it already exists, append a new entry instead of rewriting prior content.
- Each work log entry should be concise and include at least:
  - changed scope / purpose
  - key files touched
  - validation performed
  - follow-up items or risks if any remain
- Pure discussion, planning, or diagnosis without file changes does not require a work log entry.

## Final Checklist Before Hand-Off
- Commands you cite were actually checked against repo scripts/project files.
- API fields still match front/back contracts.
- No schema changes were made without explicit user confirmation.
- New pages/menus include SQL if they require DB menu setup.
- Frontend/POS text is not garbled.
- Relevant builds succeeded, or any unrun validation is clearly called out.

## PowerShell Encoding Rule
- All PowerShell file writes MUST use `UTF-8` only.
- Do NOT use `utf8BOM` / `utf-8-sig` / UTF with BOM / Unicode / UTF-16 / ANSI / Default / OEM or any other encoding for file output.
- When using `Set-Content`, `Add-Content`, `Out-File`, or redirection workflows from PowerShell, explicitly ensure the output is plain `UTF-8`.
- If a file already contains Chinese text, treat encoding safety as a first-class requirement and re-check the file after writing.
- If encoding corruption appears, stop feature work first and repair encoding before continuing.
- Any PowerShell-generated markdown, SQL, Vue, TypeScript, C#, or config file must be reviewed for garbled text before hand-off.
- Prefer `apply_patch` for file edits. Do not use PowerShell direct-write editing for normal code/doc changes unless there is no safe alternative.
- If a file can be updated with `apply_patch`, that path is mandatory over PowerShell text replacement.

## Error Memory Rule
- Repeated pitfalls and root-cause findings must be recorded in `Docs/Error.md`.
- Update `Docs/Error.md` after fixing any issue that is likely to recur, especially encoding problems, build traps, migration pitfalls, permission mismatches, or agent workflow mistakes.
- Each entry should include: date, symptom, root cause, fix, and prevention rule.

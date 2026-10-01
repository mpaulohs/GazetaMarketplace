# KIT_DEVIATIONS — log of deviations from the kit CORE (user-owned, append-only)

> **When to record here:** you were **forced to modify a kit CORE file directly** (`commands/`, `rules/` base, `agents/`, `templates/`…) that you could not override via `local/` and could not (yet) upstream. Each deviation = 1 row. On a kit upgrade, cross-check this table to **re-apply deliberately** instead of losing the change (Phase 2: the upgrade script will automatically warn about files whose hash differs from the manifest and point back here).
>
> **Before adding a new row, try in this order:** (1) override via `local/CLAUDE.local.md` / `local/rules/` — local precedence wins; (2) if the change is worth it for everyone → upstream a PR to the kit repo; (3) as a last resort → modify core + log it.

| # | Date | CORE file modified | Change (1 sentence) | Reason | Upstream status |
|---|------|------------------|-------------------|-------|---------------------|
| 1 | 2026-09-29 | `CLAUDE.md` | Added merge header (precedence vs root CLAUDE.md); "ASP.NET Core 8" -> "10" in rules table | Merge with project root CLAUDE.md | Local only |
| 2 | 2026-09-29 | `rules/tech-stack.md` | Adapted to .NET 10 / C# 14 / EF Core 10, MVC Razor frontend, Minimal APIs, MSTest + Playwright .NET, CPM; added Project Baseline section | Repo is a .NET 10 template using MSTest and CPM | Local only |
| 3 | 2026-09-29 | `rules/frontend.md` | Rewritten from Next.js/React/TypeScript to Razor Views + vanilla JS (ES modules), Fetch API, plain CSS per view, no bundler | Project forbids JS frameworks and TypeScript | Local only |
| 4 | 2026-09-29 | `agents/frontend-developer.md` | Persona rewritten from Next.js/React/TypeScript to MVC (Razor, Tag Helpers) + vanilla JS ES modules | Project forbids JS frameworks and TypeScript | Local only |
| 5 | 2026-09-29 | `rules/overrides/lang-dotnet.md` (new file) | Added .NET override: C# 14, Nullable/ImplicitUsings disabled, TreatWarningsAsErrors, naming, Async + CancellationToken, DI, records vs classes, MSTest, CPM | Kit ships no .NET override | Local only |
| 6 | 2026-09-29 | `rules/overrides/database-sqlserver.md` (new file) | Added SQL Server override: PascalCase naming, int IDENTITY PK (CreateVersion7 documented alternative), rowversion, explicit transactions, secrets handling | Kit ships no SQL Server override | Local only |
| 7 | 2026-09-29 | `hooks/post-tool-use-tracker.sh` | Call the local `node_modules/.bin/node-jq` instead of `npx -- node-jq` (3 calls per edit) | `npx` re-resolves the package each call: ~17s per Edit/Write on Windows, now ~3s | Local only (candidate for upstream) |
| 8 | 2026-09-29 | `agents/ui-ux-designer.md` | Tailwind token block/refs replaced by CSS custom properties (`wwwroot/css/base.css`) | Project uses plain CSS, no Tailwind | Local only |
| 9 | 2026-09-29 | `references/scenario-traceability.md` | "React context" example replaced by JS module state / `data-*` contract | Project uses vanilla JS | Local only |
| 10 | 2026-09-29 | `commands/arch.md` | Design-tokens reference: `tailwind.config.ts` -> CSS custom properties (`base.css`) | Project uses plain CSS | Local only |
| 11 | 2026-09-29 | `commands/plan.md` | Example files -> MVC/Razor/JS paths; handoff examples without React; xUnit+Moq -> MSTest+fakes | Project stack | Local only |
| 12 | 2026-09-29 | `rules/testing.md` | xUnit/FluentAssertions/Moq examples -> MSTest, fakes, assembly fixture, Playwright section | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 13 | 2026-09-29 | `commands/build.md` | SDK 10, fakes, Playwright frontend TDD, dotnet gate | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 14 | 2026-09-29 | `commands/test.md` | Vitest/npm -> Playwright/dotnet, TestCategory, MSTest example | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 15 | 2026-09-29 | `commands/verify.md` | tailwind breakpoints -> base.css, TestProperty, MSTest results dir | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 16 | 2026-09-29 | `commands/debug.md` | xUnit regression example -> MSTest | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 17 | 2026-09-29 | `commands/scan.md` | Trait -> TestCategory | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 18 | 2026-09-29 | `rules/git-workflow.md` | Category filter -> TestCategory | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 19 | 2026-09-29 | `agents/test-engineer.md` | MSTest/Playwright .NET stack, assembly fixture, run --project | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 20 | 2026-09-29 | `agents/backend-developer.md` | Testing stack line -> MSTest + Playwright .NET | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 21 | 2026-09-29 | `skills/tdd/SKILL.md` | xUnit/FluentAssertions examples -> MSTest | Project stack: MSTest on Microsoft.Testing.Platform, vanilla JS | Local only |
| 22 | 2026-09-29 | `commands/infra.md` | Docker images 8.0 -> 10.0 placeholders; removed Node/nginx web service and Next.js standalone steps | Project stack: .NET 10, Razor + vanilla JS | Local only |
| 23 | 2026-09-29 | `commands/docs.md` | Tech stack example -> Razor + vanilla JS, .NET 10 / C# 14 | Project stack: .NET 10, Razor + vanilla JS | Local only |
| 24 | 2026-09-29 | `references/performance-checklist.md` | Next.js section -> Razor + vanilla JS | Project stack: .NET 10, Razor + vanilla JS | Local only |
| 25 | 2026-09-29 | `skills/code-review/SKILL.md` | React re-render item -> DOM updates | Project stack: .NET 10, Razor + vanilla JS | Local only |
| 26 | 2026-09-29 | `skills/tdd/SKILL.md` | FluentAssertions mention -> MSTest assertion examples | Project stack: .NET 10, Razor + vanilla JS | Local only |
| 27 | 2026-09-29 | `rules/tech-stack.md` | Quick Reference JS rows -> Razor/CSS/Fetch/server validation; Next.js vs Vite section replaced; Mocking row | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 28 | 2026-09-29 | `rules/project-structure.md` | csproj example net8.0 -> net10.0; ImplicitUsings/Nullable disabled | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 29 | 2026-09-29 | `rules/code-style.md` | Nullable section rewritten: nullable disabled in this repo | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 30 | 2026-09-29 | `rules/clean-code.md` | C# 12 -> C# 12+ | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 31 | 2026-09-29 | `agents/backend-developer.md` | .NET 8/C# 12/EF Core 8 -> .NET 10/C# 14/EF Core 10; nullable+implicit usings disabled | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 32 | 2026-09-29 | `agents/systems-architect.md` | ASP.NET Core 8 -> 10 | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 33 | 2026-09-29 | `agents/security-auditor.md` | ASP.NET Core 8 -> 10 | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 34 | 2026-09-29 | `agents/technical-writer.md` | .NET 8 SDK -> 10 | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 35 | 2026-09-29 | `agents/code-refactor-master.md` | Dashboard.tsx example -> dashboard.js | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 36 | 2026-09-29 | `templates/TEST_REPORT_TEMPLATE.md` | Vitest -> Playwright | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 37 | 2026-09-29 | `templates/system/service-catalog.template.md` | ASP.NET Core 8 -> 10 | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 38 | 2026-09-29 | `commands/discover.md` | Multi-stack example C# 12/Core 8 -> C# 14/Core 10 | Project stack: .NET 10, Razor + vanilla JS, Nullable/ImplicitUsings disabled | Local only |
| 39 | 2026-09-30 | `rules/frontend.md` | Styling section rewritten to Bootstrap 5.3.8 (static in wwwroot/lib, no CDN/LibMan/bundler/Sass); added Third-party JavaScript (Bootstrap JS for documented components, jQuery only for MVC validation/legacy plugins); forms, accessibility, performance, folders, checklist aligned; --bs-primary vs .btn-primary note | Project decision: Bootstrap 5.3.8 replaces plain CSS | Local only |
| 40 | 2026-09-30 | `rules/tech-stack.md` | Styling and UI Components rows -> Bootstrap 5.3.8; jQuery row (MVC validation/legacy only); Frontend rows no longer list JS frameworks as alternatives; Project Baseline Frontend bullet | Project decision: Bootstrap + vanilla JS, no JS frameworks | Local only |
| 41 | 2026-09-30 | `agents/frontend-developer.md` | Persona, stack, structure, templates, checklists and red flags aligned to Bootstrap 5.3.8, Bootstrap JS for documented components, jQuery restricted | Project decision: Bootstrap + vanilla JS | Local only |
| 42 | 2026-09-30 | `commands/arch.md` | Design Tokens (2.6): --bs-* as base tokens, --app-* for extras, mapping table, .btn-primary/--bs-btn-* recolor caveat; component contracts as Bootstrap components | Project decision: Bootstrap theme via CSS variables, no Sass | Local only |
| 43 | 2026-09-30 | `agents/ui-ux-designer.md` | Token block replaced by --bs-* overrides + --app-* + .btn-primary component variables; handoff checklist adds contrast check | Project decision: Bootstrap theme via CSS variables | Local only |
| 44 | 2026-09-30 | `CLAUDE.md` | 4 lines: frontend.md rules-table row, Frontend Developer agent row, web/ folder comment, technology-names list (Next.js -> Entity Framework Core) | Remove stale frontend references | Local only |

## Sweep classification (final scan for stale frontend stack)

Decision 2026-09-30: the following mentions are **documented prohibitions, not residue**:
- "Sass" in `rules/frontend.md` (2x), `rules/tech-stack.md`, `commands/arch.md` and `agents/ui-ux-designer.md` ("no Sass", "any CSS preprocessor").
- "any CSS framework other than Bootstrap" in the `rules/frontend.md` Forbidden list and its checklist.
- The Forbidden list of JS frameworks/TypeScript in `rules/frontend.md`, and the "Do NOT introduce xUnit/NUnit" line in `rules/tech-stack.md`.

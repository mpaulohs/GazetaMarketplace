# KIT_DEVIATIONS — log of deviations from the kit CORE (user-owned, append-only)

> **When to record here:** you were **forced to modify a kit CORE file directly** (`commands/`, `rules/` base, `agents/`, `templates/`…) that you could not override via `local/` and could not (yet) upstream. Each deviation = 1 row. On a kit upgrade, cross-check this table to **re-apply deliberately** instead of losing the change (Phase 2: the upgrade script will automatically warn about files whose hash differs from the manifest and point back here).
>
> **Before adding a new row, try in this order:** (1) override via `local/CLAUDE.local.md` / `local/rules/` — local precedence wins; (2) if the change is worth it for everyone → upstream a PR to the kit repo; (3) as a last resort → modify core + log it.

| # | Date | CORE file modified | Change (1 sentence) | Reason | Upstream status |
|---|------|------------------|-------------------|-------|---------------------|
| 1 | 2026-09-29 | `CLAUDE.md` | Added merge header (precedence vs root CLAUDE.md); "ASP.NET Core 8" -> "10" in rules table | Merge with project root CLAUDE.md | Local only |
| 2 | 2026-09-29 | `rules/tech-stack.md` | Adapted to .NET 10 / C# 14 / EF Core 10, MVC Razor frontend, Minimal APIs, MSTest + Playwright .NET, CPM; added Project Baseline section | Repo is a .NET 10 template using MSTest and CPM | Local only |

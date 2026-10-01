## Project Profile

- **Mode:** greenfield   <!-- src/GazetaMarketplace.Web is still template scaffolding (HomeController + default views); no business code yet -->
- **Output Language:** Portuguese   <!-- prose, artifacts and conversation in Portuguese (pt-BR); code and identifiers stay English -->
- **Core:** C# 14 + ASP.NET Core 10 + EF Core 10 (base) → `rules/overrides/lang-dotnet.md`; .NET 10, MSTest + Microsoft.Testing.Platform, Central Package Management
- **Database:** SQL Server 2022+ (EF Core 10) → `rules/overrides/database-sqlserver.md`
- **Observability:** Serilog/Prometheus/Grafana (base)
- **Structure:** <decide in /arch — Clean Architecture (base)>
- **Frontend:** Razor Views + Bootstrap 5.3.8 + vanilla JavaScript (ES modules), sem framework JS — `src/GazetaMarketplace.Web`
- **Notes:** `Nullable` and `ImplicitUsings` are DISABLED repo-wide (`Directory.Build.props`); `TreatWarningsAsErrors=true`. `src/Example.*` and `src/ClaudeStack.*` are template samples, not product code. **Decisão do usuário (2026-09-30):** o código em `src/` é apenas esqueleto de template; o `/spec` deve tratar o repositório como greenfield mesmo com o sinal CODE positivo (não rodar `/discover`, não fazer REVERSE dos projetos de exemplo).

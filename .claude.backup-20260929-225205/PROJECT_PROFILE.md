## Project Profile

- **Mode:** greenfield   <!-- src/GazetaMarketplace.Web is still template scaffolding (HomeController + default views); no business code yet -->
- **Output Language:** Portuguese   <!-- prose, artifacts and conversation in Portuguese (pt-BR); code and identifiers stay English -->
- **Core:** C# 14 + ASP.NET Core 10 + EF Core 10 (base), .NET 10, MSTest + Microsoft.Testing.Platform, Central Package Management
- **Database:** <not declared yet — SQL Server (base); decide in /arch>
- **Observability:** Serilog/Prometheus/Grafana (base)
- **Structure:** <decide in /arch — Clean Architecture (base)>
- **Frontend:** ASP.NET Core MVC (Razor Views), `src/GazetaMarketplace.Web`
- **Notes:** `Nullable` and `ImplicitUsings` are DISABLED repo-wide (`Directory.Build.props`); `TreatWarningsAsErrors=true`. `src/Example.*` and `src/ClaudeStack.*` are template samples, not product code.

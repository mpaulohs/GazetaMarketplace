# Backlog de achados fora do escopo

- [ ] Teste de exemplo do template em `tests/GazetaMarketplace.Web.Tests.Playwright/Test1.cs` (visita playwright.dev) falha no `dotnet test`; trocar pelos testes reais de E2E — found by /build 0.1, 2026-10-02, tests/GazetaMarketplace.Web.Tests.Playwright/Test1.cs
- [ ] `dotnet format --verify-no-changes` falha nos arquivos do template (`Test1.cs` e `MSTestSettings.cs` dos dois projetos de teste: espaço em branco e BOM) — found by /build 0.1, 2026-10-02, tests/*/Test1.cs, tests/*/MSTestSettings.cs
- [ ] Tarefa 3.4 (fotos): a rota de servir fotos provavelmente precisa da mesma isenção do rate limiter global que os estáticos (`LimitadoresExtensions.PrefixosEstaticos`). Uma página com 24 cards gera >100 requisições — found by /build 0.4, 2026-10-02, src/GazetaMarketplace.Web/Seguranca/LimitadoresExtensions.cs

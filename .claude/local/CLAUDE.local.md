# Project-local rules (win over kit base and `rules/`)

Repo conventions from root `CLAUDE.md` / `Directory.Build.props` override these kit rules:

| Kit rule says | This repo does |
|---|---|
| `rules/testing.md`: xUnit + FluentAssertions + Moq, `using Xunit;` | **MSTest** (`[TestClass]`, `[TestMethod]`, `Assert.*`) on Microsoft.Testing.Platform; Playwright via `Microsoft.Playwright.MSTest.v4`. Keep the test pyramid, coverage thresholds and TestContainers guidance; translate the examples to MSTest. Run: `dotnet test` / `dotnet run --project tests/<Project>` |
| `rules/code-style.md`: `<Nullable>enable</Nullable>`, nullable annotations | **`Nullable=disable`** — no `#nullable enable`, no `string?` annotations; validate null explicitly |
| `rules/project-structure.md`: `<ImplicitUsings>enable</ImplicitUsings>` | **`ImplicitUsings=disable`** — every file declares its own `using` directives |
| Any `<PackageReference Version="...">` example | **CPM**: version only in `Directory.Packages.props` |
| `rules/code-style.md` / `naming-conventions.md` (examples in any language) | **Código em inglês, texto do usuário em português**: classes, métodos, propriedades, namespaces, pastas, arquivos, views, ViewModels/DTOs, claims, chaves de `appsettings.json`, tabelas e colunas = inglês; rotas de URL e textos de interface = português; comentários = português aceito. Tabela completa: `rules/overrides/lang-dotnet.md` §Idioma |
| `rules/clean-code.md`: C# 12 primary constructors | C# 14 (`LangVersion=latest`) — primary constructors and newer features allowed |

Build gate for `/build` and `/test`: `dotnet build` (warnings are errors) + `dotnet test`. Ask before adding a new NuGet package.

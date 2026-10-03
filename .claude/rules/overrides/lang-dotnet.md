# Override: Language — .NET (C# 14 / ASP.NET Core 10 / EF Core 10)

> **Active when** `Project Profile → Core` declares C# 14 + ASP.NET Core 10 + EF Core 10 (this repo). Read alongside `rules/clean-code.md`, `rules/code-style.md`, `rules/naming-conventions.md`, `rules/testing.md` (base — C#). This file **only records the differences** from those base rules, which were written for a generic C#/.NET 8 baseline; agnostic principles (SOLID, YAGNI, KISS, async correctness, no side effects) **remain unchanged**. Precedence: `local/` > `PROJECT_PROFILE.md` > this file > base.

---

## Language & runtime baseline

| Aspect | This repo | Notes |
|--------|-----------|-------|
| Language | **C# 14** (`LangVersion=latest`) | Modern features allowed: primary constructors, collection expressions, `field` keyword, extension members |
| Runtime / SDK | **.NET 10** (`net10.0`), pinned in `global.json` | Do not change the target framework without an ADR |
| Web | **ASP.NET Core 10** (MVC + Razor Views; Minimal APIs for JSON endpoints) | |
| ORM | **EF Core 10** | See `overrides/database-sqlserver.md` |
| Nullable reference types | **Disabled** (`<Nullable>disable</Nullable>`) | Do NOT add `#nullable enable` or `string?` annotations; validate null explicitly (`ArgumentNullException.ThrowIfNull`) |
| Implicit usings | **Disabled** (`<ImplicitUsings>disable</ImplicitUsings>`) | Every file declares its own `using` directives |
| Warnings | **`TreatWarningsAsErrors=true`** | A warning breaks the build — fix it, never suppress it without a justified `#pragma`/`NoWarn` comment |
| Packages | **Central Package Management** (`Directory.Packages.props`) | Versions live only there; `.csproj` uses `<PackageReference Include="X" />` with no `Version` |

These settings live in `Directory.Build.props`; project files must not override them.

---

## Naming conventions

| Element | Convention | Example |
|---------|------------|---------|
| Namespace, type (class/record/struct/enum/interface) | PascalCase (`I` prefix for interfaces) | `ProductService`, `IProductRepository` |
| Method, property, event, public field, constant | PascalCase | `GetByIdAsync`, `TotalPrice`, `DefaultPageSize` |
| Parameter, local variable | camelCase | `productId`, `cancellationToken` |
| **Private / internal instance field** | **`_camelCase`** | `_repository`, `_logger` |
| Private static readonly field | `s_camelCase` is NOT used — use `_camelCase` | `_options` |
| Type parameter | `T` + PascalCase | `TEntity`, `TKey` |
| Async method | PascalCase + **`Async` suffix** | `CreateOrderAsync` |
| Boolean | `Is` / `Has` / `Can` prefix | `IsActive`, `HasStock` |
| File | One public type per file; file name = type name | `ProductService.cs` |

---

## Idioma: código em inglês, texto do usuário em português

O público é brasileiro, mas o código é escrito em **inglês americano**. A regra decide pelo lugar onde o nome aparece:

| Onde | Idioma | Exemplo |
|------|--------|---------|
| Classes, interfaces, records, enums, métodos, propriedades, campos, variáveis, parâmetros, constantes | **Inglês** | `UserManagement`, `IUserManagement`, `MustChangePassword`, `failedAttempts` |
| Namespaces, pastas de código, nomes de arquivo `.cs` | **Inglês** | `Infrastructure/Identity/`, `Core/Team/`, `Core/Configuration/` |
| ViewModels e DTOs (tipo e propriedades) | **Inglês** | `ChangePasswordViewModel.NewPassword` |
| Views `.cshtml`, pastas de view, controllers | **Inglês** (nome do arquivo e da pasta); texto interno em português | `Views/Users/Index.cshtml`, `AccountController` |
| Chaves de `appsettings.json` e variáveis de ambiente | **Inglês** | `Authentication:SessionMinutes`, `Bootstrap__AdminEmail` |
| Tabelas e colunas do banco, índices, constraints | **Inglês** | `Users`, `AuditEntries`, `MustChangePassword` |
| Claims e nomes de política de autorização | **Inglês** | `must_change_password`, `full_name` |
| Códigos de erro e identificadores de log (nome da propriedade estruturada) | **Inglês** | `{UserId}` |
| Classes de teste, helpers, fakes e campos de apoio dos testes | **Inglês** | `UsersTests`, `WebFactory`, `FakeClock` |
| **Rotas de URL** | **Português** (o usuário vê na barra de endereço) | `/painel/entrar`, `/painel/usuarios` |
| **Texto de interface**: rótulo, botão, título, mensagem de erro e de sucesso, e-mail enviado | **Português** | "E-mail ou senha inválidos, ou conta desativada" |
| Comentários XML e `//` | Português é aceito (inglês também) | |

Consequências práticas:
- A rota em português é declarada de forma explícita no atributo (`[Route("painel/usuarios")]`); o nome do controller e da action seguem em inglês.
- O texto exibido ao usuário nunca é usado como identificador. Quando uma regra precisa de um nome estável (claim, código de erro, valor de papel guardado no banco), esse nome é em inglês.
- O nome de papel guardado no banco (`Administrador`, `Redator`) é **dado**, não identificador de código: o nome da constante é em inglês, o valor muda só por migration e ADR.
- Renomear um identificador nunca altera uma rota, uma tabela, uma coluna ou um texto de interface.

---

## Asynchronous code

- Every asynchronous method returns `Task` / `Task<T>` / `ValueTask<T>` (never `async void`, except event handlers) and its name **ends in `Async`**.
- Every asynchronous method that does I/O accepts a **`CancellationToken cancellationToken`** as the **last** parameter and forwards it to every awaited call (EF Core, `HttpClient`, streams). In controllers/Minimal APIs, bind it from the action parameter.
- Never block on async code (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`).
- Library/service code uses `ConfigureAwait(false)` only when it is deliberately framework-agnostic; ASP.NET Core application code does not need it.

```csharp
public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    Product product = await _repository.FindAsync(id, cancellationToken);
    return product is null ? null : ProductMapper.ToDto(product);
}
```

---

## Dependency injection

- **Constructor injection only.** No service locator (`IServiceProvider.GetService` in business code), no static access to services, no property injection.
- Depend on **interfaces** for services with behavior; register with the correct lifetime (`Scoped` for `DbContext` and per-request services, `Singleton` only for stateless/thread-safe, `Transient` for lightweight).
- Group registrations in `IServiceCollection` extension methods (`AddApplicationServices`, `AddPersistence`) called from `Program.cs`.
- Primary constructors are allowed for DI; capture the parameter into a `_camelCase` readonly field when it is used beyond initialization.
- Bind configuration with the **Options pattern** (`IOptions<T>` + validation on start), never `IConfiguration["..."]` scattered in code.

```csharp
public sealed class ProductService(IProductRepository repository, ILogger<ProductService> logger)
{
    private readonly IProductRepository _repository = repository;
    private readonly ILogger<ProductService> _logger = logger;
}
```

---

## Types: DTOs vs entities

| Kind | Type | Rules |
|------|------|-------|
| **DTOs, requests, responses, ViewModels of read-only data** | **`record`** (positional or `init`-only), immutable | `public sealed record ProductDto(int Id, string Name, decimal Price);` |
| **EF Core entities / domain objects with identity and behavior** | **`class`** | Mutable through methods that protect invariants; `private set` where possible; parameterless constructor for EF (`protected`/`private`) |
| Value objects | `readonly record struct` / `record` | Equality by value |

- Never expose EF entities from controllers, APIs or Views — map to a DTO/ViewModel.
- Prefer `sealed` on classes not designed for inheritance.
- Form-bound ViewModels that MVC must mutate during model binding may be `class` with `{ get; set; }`.

---

## Preferred language features

- **Pattern matching** over type checks/casts and chained `if`s: `is null`, `is not null`, `is { }` property patterns, `switch` expressions, relational and `and`/`or` patterns.
- **Expression-bodied members** for single-expression methods and properties.
- **File-scoped namespaces** (`namespace MyApp.Products;`) in every file.
- `var` when the type is obvious from the right-hand side; explicit type otherwise.
- Collection expressions (`[1, 2, 3]`), `using` declarations, `readonly`/`init` where it fits, `nameof` instead of string literals, string interpolation over concatenation.
- Since nullable annotations are disabled, use `is null` / `is not null` (not `== null`) for null checks.

---

## Testing

| Layer | Tool |
|-------|------|
| Unit / integration | **MSTest** on **Microsoft.Testing.Platform** (`EnableMSTestRunner=true`, `OutputType=Exe`), method-level parallelization |
| E2E | **Playwright for .NET** (`Microsoft.Playwright.MSTest.v4`) |
| Mocking | Prefer **hand-written fakes / in-memory implementations**; add a mocking library only through the Technology Decision Process (`tech-stack.md`) |
| Assertions | MSTest `Assert.*` (an assertion library is optional and needs the same approval) |

```csharp
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyApp.Tests;

[TestClass]
public class ProductServiceTests
{
    [TestMethod]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var service = new ProductService(new FakeProductRepository(), NullLogger<ProductService>.Instance);

        ProductDto result = await service.GetByIdAsync(42, CancellationToken.None);

        Assert.IsNull(result);
    }
}
```

- Test naming: `Method_Scenario_ExpectedResult`. One behavior per test, Arrange/Act/Assert.
- Run: `dotnet test` (all) or `dotnet run --project tests/<Project>` (single test project).

---

## Central Package Management

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />

<!-- Project.csproj — NO Version attribute -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
```

Add packages with `dotnet add package` (it respects CPM) or by editing both files; never put `Version=` in a `.csproj`. Ask the user before adding a new dependency.

---

## What does NOT change

`SOLID`, thin controllers, `ProblemDetails` error contract (`rules/error-handling.md`), structured logging (Serilog), parametrized queries, layering rules (`rules/project-structure.md`) and security rules (`rules/security.md`) apply exactly as in the base.

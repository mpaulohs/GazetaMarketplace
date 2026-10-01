# Testing Standards — C# / MSTest (Microsoft.Testing.Platform)

## Testing Pyramid

```
         [E2E Tests]         ← Few, slow, catch integration issues
       [Integration Tests]   ← Some, test component interaction
     [Unit Tests]            ← Many, fast, test isolated logic
```

## Test Phases — `/build` vs `/test`

The testing strategy splits across two SDLC phases (per [CLAUDE.md](../CLAUDE.md) §Development Workflow):

| Phase | Test types added & run | Dependencies | Docker? |
|-------|------------------------|--------------|---------|
| `/build` | Unit (fakes) + Integration (EF Core In-Memory / `WebApplicationFactory`) | Fakes / in-memory | ❌ Not required |
| `/test`  | Integration (TestContainers — real SQL Server / Redis / Kafka) + E2E (Playwright); also re-runs the `/build` suite | Real services in containers | ✅ Required |

→ Use **Unit / In-Memory** during `/build` for fast feedback; promote to **TestContainers / E2E** during `/test` before the `/review` gate. The integration-layer detail (InMemory vs TestContainers templates) is in §Integration Test Templates below.

## Requirements

- Unit test coverage: **minimum 80%** (scope by Mode — see §Coverage Thresholds: greenfield = whole-repo; brownfield per-change = delta-coverage + ratchet)
- All new features must have tests
- All bug fixes must have a regression test
- Tests run in CI before any merge
- Use **MSTest** (v4, on Microsoft.Testing.Platform) as the testing framework — see the `mstest-testing-platform` skill
- Use MSTest **`Assert.*` / `CollectionAssert` / `StringAssert`** for assertions (no assertion library)
- Prefer **hand-written fakes / stubs / spies** over a mocking library; adding one (Moq, NSubstitute) requires the Technology Decision Process in `tech-stack.md`
- Nullable reference types are disabled in this repo: no `?` annotations or `!` in tests

---

## Dual-Implementation Parity (MANDATORY when a rule has ≥ 2 representations)

> **Why this rule exists:** when the same rule is written in two places, they **drift independently** — and per-side tests (each side green on its own) **never** catch the drift. This bug class has escaped both `/build` and `/test` in practice (a T-SQL backfill diverged from the C# `UrlCanonicalizer` at the default-port case — only caught at `/review`). This rule turns "caught by luck" into "caught systematically".

**When it applies** — the same rule/formula exists in two representations that can drift independently:

- SQL migration **backfill** ≈ computed logic in app code (e.g. the `*Canonical` / `*Normalized` column)
- Client-side validation **mirror** ≈ server validation (JS validation mirror ↔ server-side DataAnnotations/FluentValidation)
- **Cache-key / partition-key** computed in ≥ 2 services
- **Serialize/format** on the producer ↔ parse on the consumer

**Priority order (pick 1, record the choice in the ADR/plan):**

1. **Eliminate the second representation (preferred):** the backfill CALLS the app code itself (a data-migration console / `IDesignTimeDbContextFactory` running C#) instead of reimplementing it in SQL — there is nothing left to drift.
2. **If you must reimplement → differential test is MANDATORY:** ONE test runs BOTH representations over the SAME input table and asserts each output pair is equal. The input table MUST enumerate every variant class of the rule — **each clause in the rule's definition ≥ 1 input** (e.g. URL-canonical: host-case · default-port `:80`/`:443` · non-default port · trailing slash · slash-before-query · fragment · empty path · query-case).

> **Per-side tests DO NOT replace the differential test** — two sides green on their own can still drift. For a reimplementation, "edge cases covered" means: each clause of the rule has an input pair in the parity table.

---

## Test File Organization

```
tests/
├── MyApp.UnitTests/
│   ├── Services/
│   │   ├── UserServiceTests.cs
│   │   └── OrderServiceTests.cs
│   ├── Validators/
│   │   └── CreateUserRequestValidatorTests.cs
│   └── Helpers/
│       └── DateHelperTests.cs
├── MyApp.IntegrationTests/
│   ├── Controllers/
│   │   ├── UsersControllerTests.cs
│   │   └── OrdersControllerTests.cs
│   ├── Repositories/
│   │   └── UserRepositoryTests.cs
│   └── CustomWebApplicationFactory.cs
└── MyApp.E2ETests/
    └── Scenarios/
        └── UserRegistrationTests.cs
```

---

## Unit Test Example (MSTest + fakes)

```csharp
// tests/MyApp.UnitTests/Services/UserServiceTests.cs
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyApp.UnitTests.Services;

[TestClass]
public class UserServiceTests
{
    private FakeUserRepository _repository;
    private UserService _sut; // System Under Test

    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeUserRepository();
        _sut = new UserService(_repository, NullLogger<UserService>.Instance);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        _repository.Add(new User { Id = 1, Email = "test@example.com", Name = "Test" });

        // Act
        UserDto result = await _sut.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("test@example.com", result.Email);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserNotFound_ThrowsNotFoundException()
    {
        // Act
        NotFoundException exception = await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => _sut.GetByIdAsync(42, CancellationToken.None));

        // Assert
        StringAssert.Contains(exception.Message, "42");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task CreateAsync_WhenEmailIsInvalid_ThrowsValidationException(string email)
    {
        // Arrange
        var request = new CreateUserRequest { Email = email, Name = "Test" };

        // Act + Assert
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _sut.CreateAsync(request, CancellationToken.None));
    }
}
```

> `FakeUserRepository` is a hand-written in-memory implementation — see §Test Doubles.

---

## Integration Test Templates — pick by phase

There are **two templates** for `CustomWebApplicationFactory`. Pick by which phase the test runs in. Both replace the production `DbContextOptions<AppDbContext>` registration so production connection strings are **never** touched during testing.

| Phase | Provider | Docker | Speed | Catches |
|-------|----------|--------|-------|---------|
| `/build` | `UseInMemoryDatabase` | ❌ No | ms | Logic, mapping, validation |
| `/test` | `MsSqlContainer` (TestContainers) | ✅ Yes | seconds | Collation, indexes, transactions, SQL-specific bugs |

> **Host-isolation contract (all external I/O):** Both templates spin up their own isolated dependencies (RAM-backed or container-backed). **No test may ever connect to pre-existing infrastructure** — not `localhost` SQL Server / Redis on the host machine, and not any shared DB / Redis / **Kafka broker** / email / storage / outbound endpoint referenced by connection strings in `appsettings.json`. If you see `UseSqlServer("Server=localhost;…")` — or a hosted Kafka consumer booting with the real bootstrap servers — in a test run, that's a bug. The contract is **proven, not trusted**: Gate 6 runs a whitelist connection tripwire over the captured runner output (`test.md` §Quality Gate 6) — any non-whitelisted host in the logs fails the gate. Brownfield additions:
>
> - **Hosted services:** `WebApplicationFactory` boots the real `Program.cs`, so every `IHostedService` (Kafka consumer/producer, queue worker, scheduler) starts too. The fixture MUST disable them or point them at a TestContainers-backed broker — never let them read the real `appsettings.json` values. A green suite that silently published test events to a real topic is the worst failure mode: invisible until a downstream consumer acts on garbage.
> - **Test environment config:** run the test host with `ASPNETCORE_ENVIRONMENT=Testing` + a dedicated `appsettings.Testing.json` (a NEW test-only file) containing **no real connection string** — failing fast on missing config beats silently reaching real infrastructure.
> - **Auto-migrate on startup:** legacy `Program.cs` often calls `context.Database.Migrate()` at boot — in the test host this must target the container DB (or be disabled for tests), never the configured real DB.
> - **Runtime-only override — nothing to "restore":** all replacement happens **in-memory** (`ConfigureWebHost` DI swap, env vars, the new test-only config file). NEVER edit existing production config (`appsettings.json` / `appsettings.Production.json` / `Program.cs` / `docker-compose.yml`) to make tests pass — the deployed artifact must keep its original connections exactly as-is. Proof is checkable: `git status` after the suite shows production config files unchanged (cross-checked at the gate per `CLAUDE.md` §Verification After Delegation).

### Template A — InMemory (for `/build`)

```csharp
// tests/MyApp.IntegrationTests/InMemoryWebApplicationFactory.cs
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace MyApp.IntegrationTests;

public class InMemoryWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb-{Guid.NewGuid()}"));

            // Replace Redis with in-memory distributed cache
            services.AddDistributedMemoryCache();
        });
    }
}
```

> Use this in `/build` integration tests. Untagged — runs under plain `dotnet test`. No `[TestCategory("RequiresDocker")]`.

### Template B — TestContainers (for `/test`)

> **Time optimization — assembly fixture (default):** start the factory once in an `[AssemblyInitialize]` method so that **ONE container serves the ENTIRE integration suite**. Per-class `[ClassInitialize]` = N test classes × (~30–60s SQL Server startup) wasted. Reset state between tests with Respawn / transaction rollback / unique keys per test (MSTest runs methods in parallel — see `[DoNotParallelize]` for tests that cannot share state). Fall back to per-class only when a test corrupts the container state unrecoverably.

> **arm64 (Apple Silicon):** `mcr.microsoft.com/mssql/server:2022-latest` (used in the fixture below) **has no arm64 image → segfaults under qemu**. On an arm64 machine, switch to `.WithImage("mcr.microsoft.com/azure-sql-edge:1.0.7")` + wait strategy `Wait.ForUnixContainer().UntilPortIsAvailable(1433)` (azure-sql-edge lacks `sqlcmd`, so the default `MsSqlBuilder` readiness check cannot be used). EF Core migrations apply fine on Edge for ordinary schemas; if the schema uses SQL Server-specific features missing from Edge → gate after an ADR.

> **Schema/procs defined by raw DDL (no ORM migration):** when tables / stored procedures / triggers live as DDL scripts in the repo (brownfield snapshot — locations vary, see `CODEBASE_MAP.md` §DB-object inventory), the fixture MUST execute those scripts into the container after startup, in dependency order (tables → indexes → functions/procs/triggers) — `dotnet ef database update` alone will NOT create them, and the suite would silently run against a database missing the very logic under test.

```csharp
// tests/MyApp.IntegrationTests/CustomWebApplicationFactory.cs
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace MyApp.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));
        });
    }

    public Task StartContainerAsync() => _sqlContainer.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

// tests/MyApp.IntegrationTests/IntegrationTestHost.cs
// ONE container for the whole suite — started once per test assembly
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyApp.IntegrationTests;

[TestClass]
public static class IntegrationTestHost
{
    public static CustomWebApplicationFactory Factory { get; private set; }

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext context)
    {
        Factory = new CustomWebApplicationFactory();
        await Factory.StartContainerAsync();
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        if (Factory != null)
            await Factory.DisposeAsync();
    }
}

// tests/MyApp.IntegrationTests/Controllers/UsersControllerTests.cs
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.IntegrationTests.Controllers;

[TestClass]
[TestCategory("RequiresDocker")]
public class UsersControllerTests
{
    private HttpClient _client;

    [TestInitialize]
    public void Setup()
    {
        _client = IntegrationTestHost.Factory.CreateClient();
    }

    [TestMethod]
    public async Task GetById_WhenUserExists_Returns200WithUser()
    {
        // Arrange
        int userId = await CreateTestUserAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
    }

    [TestMethod]
    public async Task GetById_WhenUserNotFound_Returns404()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/users/{int.MaxValue}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Create_WithValidData_Returns201WithUser()
    {
        // Arrange — unique data per test: methods run in parallel against the shared container
        var request = new CreateUserRequest
        {
            Email = $"test-{Guid.NewGuid()}@example.com",
            Name = "Test User",
            Password = "Password123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/users", request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(response.Headers.Location);

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(user);
        Assert.AreEqual(request.Email, user.Email);
    }

    [TestMethod]
    public async Task Create_WithInvalidEmail_Returns400()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "invalid-email",
            Name = "Test User",
            Password = "Password123!"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/users", request);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<int> CreateTestUserAsync()
    {
        using IServiceScope scope = IntegrationTestHost.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = User.Create($"test-{Guid.NewGuid()}@example.com", "Test", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }
}
```

---

## Browser & Accessibility Tests (Playwright for .NET)

The frontend is Razor + vanilla JavaScript with no bundler, so UI behavior is tested in a real browser with `Microsoft.Playwright.MSTest.v4` (`PageTest`). Query by **role > label > text > test id**, cover the core journeys **with and without JavaScript** (`JavaScriptEnabled = false`), and run an axe-core check (`Deque.AxeCore.Playwright`) on every new page. Full example: `commands/build.md` §Frontend TDD; rules: [`frontend.md`](frontend.md) §Testing.

---

## Test Commands

```bash
# Run all tests
dotnet test

# Run specific project (test projects are executables on Microsoft.Testing.Platform)
dotnet run --project tests/MyApp.UnitTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run with coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:./coverage/report

# Run specific test
dotnet test --filter "FullyQualifiedName~UserServiceTests"

# Run tests with specific category
dotnet test --filter "TestCategory=Unit"

# Watch mode
dotnet watch test --project tests/MyApp.UnitTests
```

> **Coverage under Microsoft.Testing.Platform:** this repo runs MSTest on Microsoft.Testing.Platform (`global.json` → `test.runner`). VSTest-only options (`--collect:"XPlat Code Coverage"`, `--settings coverlet.runsettings`) used in the coverage commands below may not apply; the MTP equivalent is `--coverage` (`Microsoft.Testing.Extensions.CodeCoverage`). **Confirm the working coverage command on the first `/test` run** and record it in `TEST_REPORT.md` (adding a coverage package needs the Technology Decision Process).

---

## Naming Conventions

### Test Class Names

```csharp
// Pattern: {ClassUnderTest}Tests
public class UserServiceTests { }
public class CreateUserRequestValidatorTests { }
public class UsersControllerTests { }
```

### Test Method Names

```csharp
// Pattern: {Method}_{Scenario}_{ExpectedResult}
[TestMethod]
public async Task GetByIdAsync_WhenUserExists_ReturnsUser() { }

[TestMethod]
public async Task GetByIdAsync_WhenUserNotFound_ThrowsNotFoundException() { }

[TestMethod]
public async Task CreateAsync_WithDuplicateEmail_ThrowsConflictException() { }
```

---

## Test Structure (Arrange-Act-Assert)

```csharp
[TestMethod]
public void CalculateTotal_WithDiscount_ReturnsDiscountedAmount()
{
    // Arrange - setup test data and dependencies
    var cart = new Cart();
    cart.AddItem(new CartItem { Price = 100, Quantity = 2 });
    cart.ApplyDiscount(10); // 10% discount

    // Act - execute the method under test
    var total = cart.CalculateTotal();

    // Assert - verify the result
    Assert.AreEqual(180m, total); // 200 - 10% = 180
}
```

---

## Test Doubles

### Preference Order

1. **Real implementations** (in-memory database, actual dependencies)
2. **Fakes** (in-memory implementations)
3. **Stubs** (canned responses)
4. **Spies / mocks** (verify interactions — use sparingly; hand-written spies preferred over a mocking library)

### Examples

```csharp
// 1. Real - EF Core In-Memory
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
using var context = new AppDbContext(options);

// 2. Fake - custom implementation
public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public void Add(User user) => _users.Add(user);

    public Task<User> GetByIdAsync(int id, CancellationToken cancellationToken)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}

// 3. Stub - canned response, hand-written
public class StubUserRepository : IUserRepository
{
    public User Result { get; set; }

    public Task<User> GetByIdAsync(int id, CancellationToken cancellationToken)
        => Task.FromResult(Result);

    public Task AddAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;
}

// 4. Spy - records interactions so the test can assert on them (use sparingly)
public class SpyEmailSender : IEmailSender
{
    public List<string> SentTo { get; } = new();

    public Task SendAsync(string to, string subject, CancellationToken cancellationToken)
    {
        SentTo.Add(to);
        return Task.CompletedTask;
    }
}
// ... run test ...
// Assert.AreEqual(1, spy.SentTo.Count);
```

---

## MSTest Assertion Examples

```csharp
// Basic assertions
Assert.IsNotNull(result);
Assert.AreEqual(expected, result);
Assert.AreSame(expectedInstance, result);

// Collections
Assert.AreEqual(3, users.Count);
Assert.IsTrue(users.Any(u => u.Email == "test@example.com"));
Assert.IsTrue(users.All(u => u.IsActive));
CollectionAssert.AreEqual(new[] { "Ana", "Bruno" }, users.Select(u => u.Name).ToArray());

// Strings
StringAssert.Contains(email, "@");
StringAssert.StartsWith(name, "John");
StringAssert.Matches(message, new Regex(@"User \d+ created"));

// Numbers
Assert.IsTrue(total > 0);
Assert.IsTrue(percentage >= 0 && percentage <= 100);
Assert.AreEqual(3.14, result, 0.01); // delta

// Exceptions (returns the exception so you can inspect it)
NotFoundException ex = await Assert.ThrowsExactlyAsync<NotFoundException>(
    () => _sut.GetByIdAsync(invalidId, CancellationToken.None));
StringAssert.Contains(ex.Message, "not found");

// Object comparison — compare the fields that matter (or use records for value equality)
Assert.AreEqual(expected.Name, actual.Name);
Assert.AreEqual(expected.Email, actual.Email);
```

---

## Coverage Configuration

```xml
<!-- tests/MyApp.UnitTests/MyApp.UnitTests.csproj -->
<PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura</CoverletOutputFormat>
    <Threshold>80</Threshold>
    <ThresholdType>line,branch</ThresholdType>
    <ThresholdStat>total</ThresholdStat>
</PropertyGroup>
```

### Coverage Thresholds (Quality Gate 6)

The gate reads `Project Profile → Mode` to choose the **scope** — the 80/75 numbers stay the same, the scope changes:

| Mode | GATE (blocking) | Informational (non-blocking, MUST be reported) |
|------|-------------|-------------------------------------------|
| **greenfield** | whole-repo: line ≥ 80% · branch ≥ 75% | **method %** — reported, not gating (see the zero-coverage rule below) |
| **brownfield per-change** | **delta-coverage** — computed only over the files changed/added in the change-set: line ≥ 80% · branch ≥ 75% | **whole-repo** = baseline debt, plus a **ratchet**: must not DECREASE from the previous measurement |

- **delta-coverage** = coverage filtered by `git diff --name-only <base>..HEAD` (base = merge-base with main / the previous release tag). Filter on the cobertura report or scope coverlet `Include` to the changed files.
- **Whole-repo ratchet:** record the previous run's whole-repo number in `TEST_REPORT.md §Coverage`; if the next run is **lower** than the previous one → GATE FAIL (prevents new untested code from hiding behind legacy debt). Baseline debt (e.g. R1 from `/discover`) is paid down gradually through the characterization backlog — it is **NOT** the obligation of a single PR (per `brownfield.md` §Upfront-vs-Per-change: no mass retrofit).
- `TEST_REPORT.md §Coverage` MUST record **both numbers** + the ratchet result, stating clearly which number is the gate.
- **Prerequisite:** delta needs a base commit for `git diff` → the source must be git-tracked. A repo not yet committed (e.g. a just-onboarded brownfield) → measure delta manually against the file list in `plans/plan.md` and note the measurement method in the TEST_REPORT.
- Per-file waiver (diff too small / hard to test) → record the reason in TEST_REPORT §Coverage, using the same mechanism as the exclusion-rationale in `coverlet.runsettings`.
- **Zero-coverage methods (replaces a method-percentage gate):** `TEST_REPORT.md §Coverage` MUST list **every method measured at 0%**, each with either a test added or a one-line reason. A **business-logic** method (Service / Handler / Action / domain method) at 0% with no reason → **GATE FAIL**. Structural members — auto-property, record constructor, compiler-generated equality, design-time factory (`IDesignTimeDbContextFactory`, called only by `dotnet ef`) — need only their kind named as the reason.
  **Already-shipping test:** a business-logic method at 0% that shipping code already calls, registers, or wires (e.g. a redaction transform registered into the logging pipeline) needs **a test, not a reason** — it is reachable today, so "its caller does not exist yet" does not apply. The reason form is for a method **no shipping code references yet** (a factory for an unbuilt feature, an exception type nothing throws). The two cases are separated by a reference search, so the distinction is checkable.
  *Why not a method percentage:* in .NET the method count is dominated by structural members, so a percentage produces false red (entities awaiting a later phase, a factory no test can ever call) while simultaneously hiding the real thing — a handful of untested business methods behind a crowd of covered auto-properties. The list-and-justify form catches what the percentage was meant to catch, with no structural noise.

> **Why the split by Mode:** demanding 80% whole-repo on a brownfield with a 0% test baseline forces every per-change PR to retrofit legacy — a direct conflict with `brownfield.md` (WRITE by delta). The gate is meaningful as "is the code I changed covered?"; whole-repo is a debt/trend metric, and the ratchet keeps the direction upward.

---

## Coverage Command (Local)

```bash
# Run tests with coverage locally
dotnet test \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

# Generate HTML report
reportgenerator \
  -reports:./coverage/**/coverage.cobertura.xml \
  -targetdir:./coverage/report \
  -reporttypes:Html
```

---

## Test Categories

```csharp
// Use TestCategory for categorization
[TestMethod]
[TestCategory("Unit")]
public async Task UnitTest() { }

[TestMethod]
[TestCategory("Integration")]
public async Task IntegrationTest() { }

[TestMethod]
[TestCategory("E2E")]
public async Task E2ETest() { }

// Run by category
// dotnet test --filter "TestCategory=Unit"
```

---

## Checklist

- [ ] All public methods have unit tests
- [ ] Edge cases are covered (null, empty, boundary, **wrong-type** values)

> **Wrong-type input is its own test class.** "Edge case" read as *boundary values* misses the
> input that is the wrong **shape** entirely — an object/array/number where a string is expected.
> Every externally-reachable path (HTTP body/query, message payload) MUST have at least one test
> sending a type-violating value through the real route, asserting the documented 4xx — not a
> crash. This is the test-side twin of the boundary-validation rule (`lang-nodejs.md` §Schema
> validation at the boundary · FluentValidation · Laravel FormRequest).
- [ ] Error paths are tested (exceptions, validation failures)
- [ ] Integration tests cover API endpoints
- [ ] Tests are independent (no shared state)
- [ ] Test names clearly describe what is being tested
- [ ] Coverage meets minimum threshold (80%)
- [ ] Tests run fast (< 10 seconds for unit tests)

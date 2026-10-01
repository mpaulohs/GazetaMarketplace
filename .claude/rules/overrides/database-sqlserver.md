# Override: Database — SQL Server (EF Core 10)

> **Active when** `Project Profile → Database: SQL Server` (this repo). Read alongside `rules/database.md` and `rules/naming-conventions.md` §Database Naming (base — already SQL Server + EF Core + Dapper). This file **only records project decisions and the differences/additions** for SQL Server 2022+ with EF Core 10; agnostic principles (parametrized query, projection, N+1 prevention, async, no logging of sensitive data) **remain unchanged**.

---

## §A. Engine, provider & migrations

| Aspect | Decision |
|--------|----------|
| Engine | **SQL Server 2022 or newer** (Azure SQL / SQL Server in Docker for dev and tests) |
| Provider | `Microsoft.EntityFrameworkCore.SqlServer` (EF Core 10) |
| Design-time | `Microsoft.EntityFrameworkCore.Design` — **only** in the project that hosts migrations, with `PrivateAssets="all"` |
| Migrations | Code-first with EF Core migrations, committed to source control; one migration per logical change with a descriptive name (`AddProductStockColumn`) |
| Applying | Dev: `dotnet ef database update`. Production: generate an idempotent script (`dotnet ef migrations script --idempotent`) or use a bundle — **never** `Database.Migrate()` at app startup in production |
| Retries | `EnableRetryOnFailure()` on `UseSqlServer` (transient faults) |

Packages follow Central Package Management (versions only in `Directory.Packages.props`).

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));
```

---

## §B. Naming — PascalCase (decision: PascalCase, not snake_case)

Follows `naming-conventions.md` §Database Naming and the EF Core default — **no `UseSnakeCaseNamingConvention` and no extra naming package**.

| Element | Convention | Example |
|---------|------------|---------|
| Table | PascalCase, **plural** | `Products`, `OrderItems` |
| Column | PascalCase | `CreatedAt`, `IsActive` |
| Primary key | `Id` | `Products.Id` |
| Foreign key | `{ReferencedEntity}Id` | `OrderItems.ProductId` |
| Index | `IX_{Table}_{Columns}` | `IX_Orders_UserId_CreatedAt` |
| Unique index | `UQ_{Table}_{Columns}` (`HasIndex(...).IsUnique()` + `HasDatabaseName`) | `UQ_Users_Email` |
| Constraint (FK/PK/Check) | `FK_`, `PK_`, `CK_` prefix | `FK_OrderItems_Products_ProductId` |

Entity class names are singular (`Product`); the `DbSet<Product>` property is plural (`Products`).

---

## §C. Indexes

- **Primary key → clustered index** (default). **Project default: `int IDENTITY`** — a narrow, ever-increasing key (greenfield project, no requirement for globally distributed IDs).
  - *Documented alternative:* `Guid.CreateVersion7()` (time-ordered) — **prefer `int IDENTITY`; use `CreateVersion7` only if there is an explicit requirement for a distributed ID** (e.g. IDs exposed in URLs that must not reveal business volume: then keep `int` as the clustered PK and add the GUID as a separate, unique public key).
  - **Forbidden:** random `Guid.NewGuid()` as a clustered key (index fragmentation).
- **Every foreign key column gets a non-clustered index** (EF Core creates them by convention — do not remove them).
- Add non-clustered indexes for frequent filters/sorts; use `INCLUDE` columns for covering queries and filtered indexes for soft-delete (`HasFilter("[DeletedAt] IS NULL")`).
- Verify with the execution plan; drop indexes that are unused (`sys.dm_db_index_usage_stats`).

```csharp
builder.HasKey(p => p.Id);                       // clustered PK
builder.HasIndex(p => p.CategoryId).HasDatabaseName("IX_Products_CategoryId");
builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("UQ_Products_Sku");
```

---

## §D. Queries — always parameterized

- **Never** concatenate or interpolate user input into SQL strings.
- LINQ is parameterized automatically. For raw SQL use `FromSql($"...")` / `ExecuteSqlAsync($"...")` (interpolated **is safe** here — EF turns each hole into a parameter). **Never** `FromSqlRaw`/`ExecuteSqlRaw` with concatenated strings.
- Dapper: always pass an anonymous object or `DynamicParameters` (`@Name`); never build the SQL text from input.
- Dynamic `ORDER BY` / column names: map from an allow-list, never from raw input.
- Reads that do not modify data use `AsNoTracking()` (or set `QueryTrackingBehavior.NoTracking` for read-only contexts).

---

## §E. Transactions

- A single `SaveChangesAsync` is already atomic. For **operations spanning multiple tables/aggregates or multiple `SaveChanges` calls**, use an **explicit transaction**:

```csharp
await using IDbContextTransaction transaction =
    await _context.Database.BeginTransactionAsync(cancellationToken);
try
{
    _context.Orders.Add(order);
    await _context.SaveChangesAsync(cancellationToken);

    await _stockService.ReserveAsync(order, cancellationToken);   // same DbContext
    await _context.SaveChangesAsync(cancellationToken);

    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

- With `EnableRetryOnFailure`, wrap the whole unit in `context.Database.CreateExecutionStrategy().ExecuteAsync(...)`.
- Keep transactions short; no HTTP calls or user interaction inside them. Do not use `TransactionScope` across services.

---

## §F. Optimistic concurrency — `rowversion`

Every entity that can be edited concurrently carries a `rowversion` token:

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public byte[] RowVersion { get; set; }
}

// Configuration
builder.Property(p => p.RowVersion).IsRowVersion();
```

- On `DbUpdateConcurrencyException`: reload and let the user retry/merge, or return **`409 Conflict`** as `ProblemDetails` (`rules/error-handling.md`). Never silently overwrite.
- Round-trip the token through forms/DTOs (hidden field / Base64 string) so a stale edit is detected.

---

## §G. Connection strings & secrets

- Structure lives in `appsettings.json` **without** credentials (or with a placeholder); real values come from:
  - **Development:** `dotnet user-secrets` (`ConnectionStrings:DefaultConnection`)
  - **Production/CI:** environment variables or a secret manager (Key Vault / Secrets Manager)
- Prefer **Windows/Managed Identity authentication** (`Authentication=Active Directory Default`) over SQL passwords when the platform supports it.
- Use `Encrypt=True`; `TrustServerCertificate=True` **only** for local dev containers.
- Never commit a connection string with a password; `.env`/`appsettings.*.json` with secrets are git-ignored.
- **Never log the connection string** (nor `DbConnection.ConnectionString`, nor exceptions that embed it). Keep `EnableSensitiveDataLogging()` and `EnableDetailedErrors()` **off** outside Development.
- Use a least-privilege SQL login for the app (no `sa`, no `db_owner` at runtime); migrations run with a separate, more privileged identity.

---

## What does NOT change

Repository/DbContext layering, N+1 prevention (`Include`/projection/split queries), Dapper for read-heavy queries, pagination, soft delete, and testing strategy (`rules/testing.md`, TestContainers for `/test`) apply exactly as in `rules/database.md`.

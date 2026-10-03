using System;
using System.IO;
using System.Linq;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class MigrationsTests
#pragma warning restore CA1515
{
    private static string Script() => File.ReadAllText(RepositoryHelper.Project("db/scripts/gazeta-idempotente.sql"));

    [TestMethod]
    public void Script_IncluiTodasAsMigrationsDoCodigo_ComGuardaDeIdempotencia()
    {
        using AppDbContext context = new(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=Nenhum").Options,
            new FakeCurrentUser(), new FakeClock());
        string[] migrations = context.Database.GetMigrations().ToArray();
        string script = Script();

        Assert.IsNotEmpty(migrations);
        foreach (string migration in migrations)
        {
            string guard = @"IF NOT EXISTS \(\s*SELECT \* FROM \[__EFMigrationsHistory\]\s*WHERE \[MigrationId\] = N'" + migration + @"'\s*\)";
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(script, guard),
                "Script desatualizado: regenere com 'dotnet tool run dotnet-ef migrations script --idempotent'. Falta " + migration);
        }

        StringAssert.Contains(script, "IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL");
    }

    [TestMethod]
    public void MigrationInicial_CriaSoAAuditEntries()
    {
        string section = MigrationSection("20261002230057_CreateAuditEntries");

        StringAssert.Contains(section, "CREATE TABLE [AuditEntries]");
        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(section, @"CREATE TABLE \[").Count);
    }

    [TestMethod]
    public void MigrationDoIdentity_CriaAsSeteTabelasEOsDoisPapeis()
    {
        string section = MigrationSection("20261003030830_AddIdentity");

        foreach (string table in new[] { "AspNetRoles", "AspNetUsers", "AspNetRoleClaims", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUserTokens" })
        {
            StringAssert.Contains(section, "CREATE TABLE [" + table + "]");
        }

        StringAssert.Contains(section, "[FullName] nvarchar(100) NOT NULL");
        StringAssert.Contains(section, "N''Administrador''");
        StringAssert.Contains(section, "N''Redator''");
    }

    [TestMethod]
    public void ModeloDoCodigo_EstaEmDiaComASnapshotDasMigrations()
    {
        using AppDbContext context = new(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=Nenhum").Options,
            new FakeCurrentUser(), new FakeClock());

        Assert.IsFalse(context.Database.HasPendingModelChanges(), "O modelo mudou sem migration: gere uma nova com 'dotnet-ef migrations add' (uma por mudança lógica).");
    }

    // Isola o trecho de uma migration no script idempotente: da guarda dela até a guarda da próxima
    private static string MigrationSection(string migration)
    {
        string script = Script();
        int start = script.IndexOf("WHERE [MigrationId] = N'" + migration + "'", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, "migration ausente do script: " + migration);
        int end = script.IndexOf("COMMIT;", start, StringComparison.Ordinal);
        return script.Substring(start, end - start);
    }

    [TestMethod]
    public void Codigo_NaoUsaDatabaseMigrate()
    {
        foreach (string file in Directory.EnumerateFiles(RepositoryHelper.Project("src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            Assert.IsFalse(text.Contains("Database.Migrate(", StringComparison.Ordinal)
                || text.Contains("Database.MigrateAsync(", StringComparison.Ordinal),
                Path.GetFileName(file) + " aplica migrations na partida (ADR-004)");
        }
    }
}

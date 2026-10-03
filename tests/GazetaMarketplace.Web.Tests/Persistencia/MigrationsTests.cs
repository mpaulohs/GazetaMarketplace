using System;
using System.IO;
using System.Linq;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistencia;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class MigrationsTests
#pragma warning restore CA1515
{
    private static string Script() => File.ReadAllText(RepositorioHelper.Projeto("db/scripts/gazeta-idempotente.sql"));

    [TestMethod]
    public void Script_IncluiTodasAsMigrationsDoCodigo_ComGuardaDeIdempotencia()
    {
        using AppDbContext contexto = new(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=Nenhum").Options,
            new UsuarioFalso(), new RelogioFalso());
        string[] migrations = contexto.Database.GetMigrations().ToArray();
        string script = Script();

        Assert.IsNotEmpty(migrations);
        foreach (string migration in migrations)
        {
            string guarda = @"IF NOT EXISTS \(\s*SELECT \* FROM \[__EFMigrationsHistory\]\s*WHERE \[MigrationId\] = N'" + migration + @"'\s*\)";
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(script, guarda),
                "Script desatualizado: regenere com 'dotnet tool run dotnet-ef migrations script --idempotent'. Falta " + migration);
        }

        StringAssert.Contains(script, "IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL");
    }

    [TestMethod]
    public void MigrationInicial_CriaSoAAuditEntries()
    {
        string trecho = TrechoDaMigration("20261002230057_CriarAuditEntries");

        StringAssert.Contains(trecho, "CREATE TABLE [AuditEntries]");
        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(trecho, @"CREATE TABLE \[").Count);
    }

    [TestMethod]
    public void MigrationDoIdentity_CriaAsSeteTabelasEOsDoisPapeis()
    {
        string trecho = TrechoDaMigration("20261003030830_AdicionarIdentity");

        foreach (string tabela in new[] { "AspNetRoles", "AspNetUsers", "AspNetRoleClaims", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUserTokens" })
        {
            StringAssert.Contains(trecho, "CREATE TABLE [" + tabela + "]");
        }

        StringAssert.Contains(trecho, "[FullName] nvarchar(100) NOT NULL");
        StringAssert.Contains(trecho, "N''Administrador''");
        StringAssert.Contains(trecho, "N''Redator''");
    }

    [TestMethod]
    public void ModeloDoCodigo_EstaEmDiaComASnapshotDasMigrations()
    {
        using AppDbContext contexto = new(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=Nenhum").Options,
            new UsuarioFalso(), new RelogioFalso());

        Assert.IsFalse(contexto.Database.HasPendingModelChanges(), "O modelo mudou sem migration: gere uma nova com 'dotnet-ef migrations add' (uma por mudança lógica).");
    }

    // Isola o trecho de uma migration no script idempotente: da guarda dela até a guarda da próxima
    private static string TrechoDaMigration(string migration)
    {
        string script = Script();
        int inicio = script.IndexOf("WHERE [MigrationId] = N'" + migration + "'", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, inicio, "migration ausente do script: " + migration);
        int fim = script.IndexOf("COMMIT;", inicio, StringComparison.Ordinal);
        return script.Substring(inicio, fim - inicio);
    }

    [TestMethod]
    public void Codigo_NaoUsaDatabaseMigrate()
    {
        foreach (string arquivo in Directory.EnumerateFiles(RepositorioHelper.Projeto("src"), "*.cs", SearchOption.AllDirectories))
        {
            if (arquivo.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            string texto = File.ReadAllText(arquivo);
            Assert.IsFalse(texto.Contains("Database.Migrate(", StringComparison.Ordinal)
                || texto.Contains("Database.MigrateAsync(", StringComparison.Ordinal),
                Path.GetFileName(arquivo) + " aplica migrations na partida (ADR-004)");
        }
    }
}

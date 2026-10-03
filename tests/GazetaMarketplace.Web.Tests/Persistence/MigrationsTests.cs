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
    public void Script_LigaQuotedIdentifierNoTopo_AntesDeQualquerComando()
    {
        // Os índices filtrados exigem QUOTED_IDENTIFIER ON; sem o SET o sqlcmd (sem -I) falha com o erro 1934
        string[] code = [.. Script().TrimStart('\uFEFF').Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("--", StringComparison.Ordinal))];

        Assert.AreEqual("SET QUOTED_IDENTIFIER ON;", code[0]);
        Assert.AreEqual("GO", code[1]);
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
    public void MigrationDaRecuperacaoDeSenha_CriaATabelaDeTentativasComOsIndices()
    {
        string section = MigrationSection("20261003081958_AddPasswordRecoveryAttempts");

        StringAssert.Contains(section, "CREATE TABLE [PasswordRecoveryAttempts]");
        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(section, @"CREATE TABLE \[").Count);
        StringAssert.Contains(section, "CREATE INDEX [IX_PasswordRecoveryAttempts_Email_RequestedAt] ON [PasswordRecoveryAttempts] ([Email], [RequestedAt])");
        StringAssert.Contains(section, "CREATE INDEX [IX_PasswordRecoveryAttempts_Ip_RequestedAt] ON [PasswordRecoveryAttempts] ([Ip], [RequestedAt])");
        StringAssert.Contains(section, "CREATE INDEX [IX_PasswordRecoveryAttempts_RequestedAt] ON [PasswordRecoveryAttempts] ([RequestedAt])");
    }

    [TestMethod]
    public void MigrationDeCategorias_CriaATabelaComIndicesEACargaInicial()
    {
        string section = MigrationSection(Migrations.Single(m => m.EndsWith("_AddCategories", StringComparison.Ordinal)));

        StringAssert.Contains(section, "CREATE TABLE [Categories]");
        StringAssert.Contains(section, "CREATE UNIQUE INDEX [UQ_Categories_Slug]");
        StringAssert.Contains(section, "CREATE UNIQUE INDEX [UQ_Categories_ParentId_Name] ON [Categories] ([ParentId], [Name]) WHERE [ParentId] IS NOT NULL");
        StringAssert.Contains(section, "CREATE UNIQUE INDEX [UQ_Categories_Name_Root] ON [Categories] ([Name]) WHERE [ParentId] IS NULL");
        StringAssert.Contains(section, "CREATE INDEX [IX_Categories_ParentId_DisplayOrder]");
        StringAssert.Contains(section, "CK_Categories_NotOwnParent");
        StringAssert.Contains(section, "SET IDENTITY_INSERT [Categories] ON");
        StringAssert.Contains(section, "N''Autopeças''");
        // 147 linhas de carga (cada uma leva a data fixa da carga); o animal vivo 79 não entra
        Assert.AreEqual(147, System.Text.RegularExpressions.Regex.Matches(section, "''2026-10-03T00:00:00.0000000Z''").Count);
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(section, @"(VALUES |\n    )\(79, "), "Cachorros (79) fica fora da v1");
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(section, @"(VALUES |\n    )\(155, "), "Papelaria (155) entra com o id real");
    }

    [TestMethod]
    public void MigrationDosGruposDeCampos_AtualizaSoAsSeisCategoriasQueDefinemGrupo()
    {
        string section = MigrationSection(Migrations.Single(m => m.EndsWith("_AssignFieldGroupsToRealEstateServicesAndJobs", StringComparison.Ordinal)));

        Assert.AreEqual(6, System.Text.RegularExpressions.Regex.Matches(section, @"UPDATE \[Categories\] SET \[FieldGroup\]").Count);
        foreach ((int id, string group) in new[] { (26, "RealEstate"), (27, "RealEstate"), (30, "RealEstate"), (31, "RealEstate"), (66, "Services"), (96, "Jobs") })
        {
            StringAssert.Contains(section.Replace("\r", string.Empty, StringComparison.Ordinal), $"[FieldGroup] = ''{group}''\n    WHERE [Id] = {id};");
        }
    }

    [TestMethod]
    public void MigrationDosGruposDeVeiculosEPecas_AtualizaSoAsSeisCategorias()
    {
        string section = MigrationSection(Migrations.Single(m => m.EndsWith("_AssignFieldGroupsToVehiclesAndParts", StringComparison.Ordinal)));

        Assert.AreEqual(6, System.Text.RegularExpressions.Regex.Matches(section, @"UPDATE \[Categories\] SET \[FieldGroup\]").Count);
        foreach ((int id, string group) in new[] { (3, "Parts"), (33, "Cars"), (34, "TrucksAndBuses"), (35, "TrucksAndBuses"), (36, "Motorcycles"), (37, "BoatsAndAircraft") })
        {
            StringAssert.Contains(section.Replace("\r", string.Empty, StringComparison.Ordinal), $"[FieldGroup] = ''{group}''\n    WHERE [Id] = {id};");
        }
    }

    private static string[] Migrations
    {
        get
        {
            using AppDbContext context = new(
                new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(local);Database=Nenhum").Options,
                new FakeCurrentUser(), new FakeClock());
            return [.. context.Database.GetMigrations()];
        }
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

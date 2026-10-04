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

    [TestMethod]
    public void MigrationDosGruposRestantes_Atualiza53CategoriasEFechaOsDezoitoGrupos()
    {
        string section = MigrationSection(Migrations.Single(m => m.EndsWith("_AssignFieldGroupsToRemainingCategories", StringComparison.Ordinal))).Replace("\r", string.Empty, StringComparison.Ordinal);
        (int Id, string Group)[] expected =
        [
            (28, "RoomRental"), (29, "Seasonal"), (43, "Phones"), (46, "Smartwatches"), (44, "TelephonyProducts"), (45, "TelephonyProducts"),
            (47, "TelephonyProducts"), (48, "TelephonyProducts"), (64, "ClothingAndShoes"), (65, "ClothingAndShoes"), (68, "ClothingAndShoes"),
            (69, "ClothingAndShoes"), (72, "ClothingAndShoes"), (75, "ClothingAndShoes"), (76, "ClothingAndShoes"), (77, "ClothingAndShoes"),
            (89, "Machinery"), (92, "Machinery"), (93, "Machinery"), (97, "Machinery"),
            .. System.Linq.Enumerable.Range(102, 26).Select(id => (id, "ElectronicsAndComputers")),
            .. System.Linq.Enumerable.Range(128, 7).Select(id => (id, "Appliances"))
        ];

        Assert.HasCount(53, expected);
        Assert.AreEqual(53, System.Text.RegularExpressions.Regex.Matches(section, @"UPDATE \[Categories\] SET \[FieldGroup\]").Count);
        Assert.DoesNotContain("INSERT INTO [Categories]", section, "só atribui grupos; nenhuma categoria nasce nem some");
        foreach ((int id, string group) in expected)
        {
            StringAssert.Contains(section, $"[FieldGroup] = ''{group}''\n    WHERE [Id] = {id};");
        }
    }

    [TestMethod]
    public void MigrationDosAnuncios_CriaAdsEAdPhotos_ComColunasCalculadasChecksEIndicesDoArchitecture()
    {
        string section = MigrationSection(Migrations.Single(m => m.EndsWith("_AddAds", StringComparison.Ordinal))).Replace("\r", string.Empty, StringComparison.Ordinal);

        Assert.AreEqual(2, System.Text.RegularExpressions.Regex.Matches(section, @"CREATE TABLE \[").Count);
        StringAssert.Contains(section, "CREATE TABLE [Ads]");
        StringAssert.Contains(section, "CREATE TABLE [AdPhotos]");

        // As cinco colunas calculadas persistidas, com TRY_CAST (emenda do ADR-002)
        foreach ((string column, string path, string type) in new[] { ("VehicleBrandId", "brandId", "int"), ("VehicleModelId", "modelId", "int"), ("ModelYear", "modelYear", "int"), ("Km", "km", "int"), ("AreaM2", "areaM2", "decimal(12,2)") })
        {
            StringAssert.Contains(section, $"[{column}] AS TRY_CAST(JSON_VALUE([Attributes], '$.{path}') AS {type}) PERSISTED", column);
        }

        Assert.DoesNotContain("AS CAST(", section, "CAST derrubaria o INSERT com um valor malformado no JSON");
        StringAssert.Contains(section, "ISJSON([Attributes]) = 1 AND LEFT(LTRIM([Attributes]), 1) = '{'");
        StringAssert.Contains(section, "[Status] BETWEEN 1 AND 5");
        StringAssert.Contains(section, "[PriceCents] > 0 AND [PriceCents] <= 9999999999");
        StringAssert.Contains(section, "LEN([Description]) <= 6000");

        foreach (string index in new[]
        {
            "IX_Ads_Status_CategoryId_PublishedAt", "IX_Ads_Status_Uf_City", "IX_Ads_Status_PriceCents", "IX_Ads_VehicleBrandId_ModelYear", "IX_Ads_Km", "IX_Ads_AreaM2",
            "IX_Ads_AuthorId_Status_UpdatedAt", "IX_Ads_CategoryId", "IX_Ads_PublishedById", "IX_Ads_RejectedById", "IX_AdPhotos_AdId_SortOrder", "UQ_AdPhotos_StorageKey"
        })
        {
            StringAssert.Contains(section, $"[{index}]", index);
        }

        StringAssert.Contains(section, "[PriceCents] IS NOT NULL", "índice de preço filtrado: Serviços ficam de fora");
        StringAssert.Contains(section, "[PublishedAt] DESC");
        Assert.DoesNotContain("ON DELETE CASCADE", section, "nenhuma exclusão em cascata");
        Assert.DoesNotContain("INSERT INTO [Ads]", section, "nenhum anúncio de exemplo");
        Assert.DoesNotContain("INSERT INTO [AdPhotos]", section);
        Assert.DoesNotContain("[CategoryId] int NOT NULL", section, "rascunho só com o título não tem categoria (D1)");
    }

    [TestMethod]
    public void MigrationDoCepEMunicipios_CriaSoCepCacheECities_SemLinhasENadaDeRuaOuBairro()
    {
        string section = MigrationSection(Migrations.Single(m => m.EndsWith("_AddCepCacheAndCities", StringComparison.Ordinal))).Replace("\r", string.Empty, StringComparison.Ordinal);

        Assert.AreEqual(2, System.Text.RegularExpressions.Regex.Matches(section, @"CREATE TABLE \[").Count);
        StringAssert.Contains(section, "CREATE TABLE [CepCache]");
        StringAssert.Contains(section, "CONSTRAINT [PK_CepCache] PRIMARY KEY ([Cep])");
        StringAssert.Contains(section, "[Cep] char(8) NOT NULL");
        StringAssert.Contains(section, "[Cep] NOT LIKE '%[^0-9]%' AND LEN([Cep]) = 8");
        StringAssert.Contains(section, "CREATE TABLE [Cities]");
        StringAssert.Contains(section, "CONSTRAINT [PK_Cities] PRIMARY KEY ([IbgeCode])");
        StringAssert.Contains(section, "CREATE UNIQUE INDEX [UQ_Cities_Uf_NameSearch] ON [Cities] ([Uf], [NameSearch])");
        Assert.DoesNotContain("IDENTITY", section, "o código do IBGE nunca é gerado");
        Assert.DoesNotContain("RowVersion", section, "tabelas de referência: sem concorrência otimista");
        foreach (string street in new[] { "Logradouro", "Bairro", "Street", "Neighborhood" })
        {
            Assert.DoesNotContain(street, section, System.StringComparison.OrdinalIgnoreCase, "NFR-19: o CEP só guarda cidade, UF e código do IBGE");
        }

        Assert.DoesNotContain("INSERT INTO [Cities]", section, "a carga dos municípios é por script à parte (tools/CitiesImport)");
        Assert.DoesNotContain("INSERT INTO [CepCache]", section);
    }

    [TestMethod]
    public void MigrationDasConfiguracoes_CriaSoASiteSettings_ComChaveUnica_ESemLinhasIniciais()
    {
        string section = MigrationSection(Migrations.Single(m => m.EndsWith("_AddSiteSettings", StringComparison.Ordinal)));

        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(section, @"CREATE TABLE \[").Count);
        StringAssert.Contains(section, "CREATE TABLE [SiteSettings]");
        StringAssert.Contains(section, "CREATE UNIQUE INDEX [UQ_SiteSettings_Key] ON [SiteSettings] ([Key])");
        Assert.DoesNotContain("INSERT INTO [SiteSettings]", section, "o telefone nasce vazio: o Administrador precisa informá-lo (US-015)");
    }

    [TestMethod]
    public void MigrationDoCatalogoDeVeiculos_CriaAsQuatroTabelas_ComChavesCompostasEOTipoEmTodas_ESemLinhas()
    {
        string section = MigrationSection(Migrations.Single(m => m.EndsWith("_AddVehicleCatalog", StringComparison.Ordinal))).Replace("\r", string.Empty, StringComparison.Ordinal);

        Assert.AreEqual(4, System.Text.RegularExpressions.Regex.Matches(section, @"CREATE TABLE \[").Count);
        StringAssert.Contains(section, "CONSTRAINT [PK_VehicleBrands] PRIMARY KEY ([Id], [Kind])");
        StringAssert.Contains(section, "CONSTRAINT [PK_VehicleModels] PRIMARY KEY ([Id], [Kind])");
        StringAssert.Contains(section, "CONSTRAINT [PK_VehicleModelYears] PRIMARY KEY ([ModelId], [Year], [Kind])");
        StringAssert.Contains(section, "CONSTRAINT [PK_VehicleVersions] PRIMARY KEY ([Id], [Kind])");
        StringAssert.Contains(section, "FOREIGN KEY ([BrandId], [Kind]) REFERENCES [VehicleBrands] ([Id], [Kind])");
        StringAssert.Contains(section, "FOREIGN KEY ([ModelId], [Kind]) REFERENCES [VehicleModels] ([Id], [Kind])");
        StringAssert.Contains(section, "FOREIGN KEY ([ModelId], [Year], [Kind]) REFERENCES [VehicleModelYears] ([ModelId], [Year], [Kind])");
        Assert.AreEqual(4, System.Text.RegularExpressions.Regex.Matches(section, @"\[Kind\] IN \('car', 'moto'\)").Count, "o tipo só pode ser car ou moto, em todas as tabelas");
        Assert.DoesNotContain("IDENTITY", section, "os ids vêm da origem, nunca são gerados");
        Assert.DoesNotContain("INSERT INTO [Vehicle", section, "as tabelas nascem vazias: a carga é um passo à parte");
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

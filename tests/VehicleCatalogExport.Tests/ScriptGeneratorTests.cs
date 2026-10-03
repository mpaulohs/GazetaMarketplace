using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VehicleCatalogExport.Tests;

/// <summary>O texto do script de carga: ordem, idempotência por construção, lotes, escape e repetibilidade.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ScriptGeneratorTests
#pragma warning restore CA1515
{
    private static CatalogData Small() => CatalogValidator.Validate([Raw.Complete(Kind.Car), Raw.Complete(Kind.Moto, id: 1)]).Data;

    [TestMethod]
    public void Script_ComecaLigandoQuotedIdentifier_EFicaNumaTransacao()
    {
        string script = ScriptGenerator.Script(Small(), "teste");
        string[] code = [.. Raw.Lines(script).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("--", System.StringComparison.Ordinal))];

        Assert.AreEqual("SET QUOTED_IDENTIFIER ON;", code[0]);
        Assert.AreEqual("GO", code[1]);
        CollectionAssert.Contains(code, "BEGIN TRANSACTION;");
        Assert.AreEqual("COMMIT TRANSACTION;", code[^2]);
    }

    [TestMethod]
    public void Script_CarregaNaOrdemMarcasModelosAnosVersoes_ComMerge()
    {
        string script = ScriptGenerator.Script(Small(), "teste");

        int brands = script.IndexOf("MERGE [VehicleBrands]", System.StringComparison.Ordinal);
        int models = script.IndexOf("MERGE [VehicleModels]", System.StringComparison.Ordinal);
        int years = script.IndexOf("MERGE [VehicleModelYears]", System.StringComparison.Ordinal);
        int versions = script.IndexOf("MERGE [VehicleVersions]", System.StringComparison.Ordinal);
        Assert.IsTrue(0 <= brands && brands < models && models < years && years < versions, "cada nível depende do anterior");
    }

    [TestMethod]
    public void Script_NuncaApagaNada_EAChaveDoMergeLevaOTipo()
    {
        string script = ScriptGenerator.Script(Small(), "teste");

        foreach (string forbidden in new[] { "DELETE", "DROP", "TRUNCATE", "ALTER" })
        {
            Assert.DoesNotContain(forbidden, script, forbidden);
        }

        StringAssert.Contains(script, "ON t.[Id] = s.[Id] AND t.[Kind] = s.[Kind]");
        StringAssert.Contains(script, "ON t.[ModelId] = s.[ModelId] AND t.[Year] = s.[Year] AND t.[Kind] = s.[Kind]");
    }

    [TestMethod]
    public void Script_GravaAOrigemEmTodaLinha_EAtualizaQuandoAOrigemMuda()
    {
        string script = ScriptGenerator.Script(Small(), "gazetaonline-2026-09");

        StringAssert.Contains(script, "VALUES (s.[Id], s.[Kind], s.[Name], 'gazetaonline-2026-09')");
        StringAssert.Contains(script, "t.[Source] <> 'gazetaonline-2026-09'");
        StringAssert.Contains(script, "[Source] = 'gazetaonline-2026-09'");
    }

    [TestMethod]
    public void Script_DuplicaAspasENomes_ComUnicode()
    {
        CatalogData data = CatalogValidator.Validate([Raw.Catalog(Kind.Car, brands: [new RawBrand(1, "D'Água Ação")])]).Data;

        string script = ScriptGenerator.Script(data, "teste");

        StringAssert.Contains(script, "N'D''Água Ação'");
    }

    [TestMethod]
    public void Script_NaoCarregaTextoDeFora_ComoComando()
    {
        // Um nome malicioso vira literal inofensivo: a aspa é duplicada e o texto fica dentro do literal
        CatalogData data = CatalogValidator.Validate([Raw.Catalog(Kind.Car, brands: [new RawBrand(1, "x'); DROP TABLE VehicleBrands;--")])]).Data;

        string script = ScriptGenerator.Script(data, "teste");

        StringAssert.Contains(script, "N'x''); DROP TABLE VehicleBrands;--'");
    }

    [TestMethod]
    public void Script_EDeterministico_OMesmoCatalogoGeraOMesmoTexto()
    {
        Assert.AreEqual(ScriptGenerator.Script(Small(), "teste"), ScriptGenerator.Script(Small(), "teste"));
    }

    [TestMethod]
    public void Script_NaoDependeDaOrdemDeEntrada()
    {
        CatalogData data = Small();
        CatalogData reversed = new([.. data.Brands.Reverse()], [.. data.Models.Reverse()], [.. data.Years.Reverse()], [.. data.Versions.Reverse()]);

        Assert.AreEqual(ScriptGenerator.Script(data, "teste"), ScriptGenerator.Script(reversed, "teste"));
    }

    [TestMethod]
    public void Lotes_TemNoMaximo500Linhas()
    {
        List<RawBrand> brands = [.. Enumerable.Range(1, 1201).Select(i => new RawBrand(i, "Marca " + i))];
        CatalogData data = CatalogValidator.Validate([Raw.Catalog(Kind.Car, brands: brands)]).Data;

        string[] statements = [.. ScriptGenerator.Statements(data, "teste")];

        Assert.HasCount(3, statements);
        Assert.IsTrue(statements.All(s => s.Split("N'Marca ").Length - 1 <= ScriptGenerator.BatchSize));
        Assert.AreEqual(1201, statements.Sum(s => s.Split("N'Marca ").Length - 1));
    }

    [TestMethod]
    public void Cabecalho_TrazAsContagens()
    {
        StringAssert.Contains(ScriptGenerator.Script(Small(), "teste"), "-- Marcas: 2 | Modelos: 2 | Anos: 2 | Versões: 2");
    }
}

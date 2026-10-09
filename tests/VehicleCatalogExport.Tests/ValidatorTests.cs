using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VehicleCatalogExport.Tests;

/// <summary>A conferência da hierarquia: o que entra, o que é descartado e o que o relatório diz.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ValidatorTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void CatalogoCompleto_EntraInteiro_SemDescartes()
    {
        ValidationResult result = CatalogValidator.Validate([Raw.Complete(Kind.Car)]);

        Assert.AreEqual(1, result.Data.Brands.Count);
        Assert.AreEqual(1, result.Data.Models.Count);
        Assert.AreEqual(1, result.Data.Years.Count);
        Assert.AreEqual(1, result.Data.Versions.Count);
        Assert.IsEmpty(result.Orphans);
        Assert.AreEqual("Nenhum registro descartado.\n", CatalogValidator.Report(result));
    }

    [TestMethod]
    public void Orfaos_SaoDescartadosERelatados_ComOMotivo()
    {
        RawCatalog raw = Raw.Catalog(
            Kind.Car,
            brands: [new RawBrand(1, "Honda")],
            models: [new RawModel(1, 1, "Civic"), new RawModel(2, 99, "Modelo sem marca"), new RawModel(3, null, "Modelo de marca nula")],
            years: [new RawYear(1, 1, 2019), new RawYear(2, 77, 2019), new RawYear(3, null, 2019)],
            versions: [new RawVersion(1, 1, 2019, "EX"), new RawVersion(2, null, null, "Sem ano"), new RawVersion(3, 1, 2005, "Ano que o modelo não tem")]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        Assert.AreEqual(1, result.Data.Models.Count);
        Assert.AreEqual(1, result.Data.Years.Count);
        Assert.AreEqual(1, result.Data.Versions.Count);
        CollectionAssert.AreEquivalent(
            new[] { "modelo|2|sem marca", "modelo|3|sem marca", "ano|77/2019|sem modelo", "ano|/2019|sem modelo", "versão|2|sem ano", "versão|3|sem ano" },
            result.Orphans.Select(o => $"{o.Table}|{o.Key}|{o.Reason}").ToArray());
        StringAssert.StartsWith(CatalogValidator.Report(result), "Registros descartados: 6\n");
    }

    [TestMethod]
    public void QuemPerdeOPai_PerdeOsFilhosEmCascata()
    {
        // A marca 1 não tem nome: sai, e com ela o modelo, o ano e a versão que dependiam dela
        RawCatalog raw = Raw.Catalog(
            Kind.Car,
            brands: [new RawBrand(1, "  ")],
            models: [new RawModel(1, 1, "Civic")],
            years: [new RawYear(1, 1, 2019)],
            versions: [new RawVersion(1, 1, 2019, "EX")]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        Assert.IsEmpty(result.Data.Brands);
        Assert.IsEmpty(result.Data.Models);
        Assert.IsEmpty(result.Data.Years);
        Assert.IsEmpty(result.Data.Versions);
        CollectionAssert.AreEqual(new[] { "marca", "modelo", "ano", "versão" }, result.Orphans.Select(o => o.Table).ToArray());
    }

    [TestMethod]
    public void IdsRepetidosNoMesmoTipo_FicamComOPrimeiro_ENoRelatorio()
    {
        RawCatalog raw = Raw.Catalog(Kind.Car, brands: [new RawBrand(1, "Honda"), new RawBrand(1, "Outra Honda")]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        Assert.AreEqual("Honda", result.Data.Brands.Single().Name);
        Assert.AreEqual("id repetido", result.Orphans.Single().Reason);
    }

    [TestMethod]
    public void AnoRepetidoNoModelo_EntraUmaVezSo()
    {
        RawCatalog raw = Raw.Catalog(
            Kind.Car,
            brands: [new RawBrand(1, "Honda")],
            models: [new RawModel(1, 1, "Civic")],
            years: [new RawYear(1, 1, 2019), new RawYear(2, 1, 2019)]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        Assert.AreEqual(1, result.Data.Years.Count);
        Assert.AreEqual("ano repetido no modelo", result.Orphans.Single().Reason);
    }

    [TestMethod]
    public void MesmoIdEmCarroEMoto_NaoSeConfundem_ECadaLinhaLevaOSeuTipo()
    {
        ValidationResult result = CatalogValidator.Validate([Raw.Complete(Kind.Car), Raw.Complete(Kind.Moto)]);

        Assert.IsEmpty(result.Orphans, "Id 1 existir nas duas origens não é repetição");
        CollectionAssert.AreEquivalent(new[] { Kind.Car, Kind.Moto }, result.Data.Brands.Select(b => b.Kind).ToArray());
        Assert.IsTrue(result.Data.Versions.All(v => v.Id == 1));
        Assert.AreEqual(2, result.Data.Versions.Count);
    }

    [TestMethod]
    public void UmModeloSoNoCarro_NaoAceitaMarcaDaMoto()
    {
        // A marca 5 só existe nas motos; o modelo de carros que aponta para a marca 5 é órfão
        RawCatalog cars = Raw.Catalog(Kind.Car, brands: [new RawBrand(1, "Honda")], models: [new RawModel(1, 5, "Modelo")]);
        RawCatalog motos = Raw.Catalog(Kind.Moto, brands: [new RawBrand(5, "BMW")]);

        ValidationResult result = CatalogValidator.Validate([cars, motos]);

        Assert.IsEmpty(result.Data.Models);
        Assert.AreEqual("sem marca", result.Orphans.Single().Reason);
    }

    [TestMethod]
    public void Nomes_SaoAparados()
    {
        ValidationResult result = CatalogValidator.Validate([Raw.Catalog(Kind.Car, brands: [new RawBrand(1, "  Honda ")])]);

        Assert.AreEqual("Honda", result.Data.Brands.Single().Name);
    }

    [TestMethod]
    public void AnoSemVersoes_ModeloSemAnos_EMarcaSemModelos_SaoValidos()
    {
        RawCatalog raw = Raw.Catalog(
            Kind.Car,
            brands: [new RawBrand(1, "Honda"), new RawBrand(2, "Marca vazia")],
            models: [new RawModel(1, 1, "Com anos"), new RawModel(2, 1, "Sem anos")],
            years: [new RawYear(1, 1, 2019)]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        Assert.IsEmpty(result.Orphans, "ter filhos não é obrigatório");
        Assert.AreEqual(2, result.Data.Brands.Count);
        Assert.AreEqual(2, result.Data.Models.Count);
        Assert.AreEqual(1, result.Data.Years.Count);
        Assert.IsEmpty(result.Data.Versions);
    }

    // ---------- limites do site (colunas e API) e texto corrompido na origem ----------

    [TestMethod]
    public void Nomes_AcimaDoLimiteDaColunaDoSite_SaoDescartados_NaoTruncados()
    {
        RawCatalog raw = Raw.Catalog(
            Kind.Car,
            brands: [new RawBrand(1, new string('a', 150)), new RawBrand(2, new string('a', 151))],
            models: [new RawModel(1, 1, new string('m', 150)), new RawModel(2, 1, new string('m', 151))],
            years: [new RawYear(1, 1, 2019)],
            versions: [new RawVersion(1, 1, 2019, new string('v', 250)), new RawVersion(2, 1, 2019, new string('v', 251))]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        Assert.AreEqual(1, result.Data.Brands.Count);
        Assert.AreEqual(1, result.Data.Models.Count);
        Assert.AreEqual(1, result.Data.Versions.Count);
        CollectionAssert.AreEquivalent(
            new[] { "marca|2|nome longo demais", "modelo|2|nome longo demais", "versão|2|nome longo demais" },
            result.Orphans.Select(o => $"{o.Table}|{o.Key}|{o.Reason}").ToArray());
    }

    [TestMethod]
    public void Ids_ZeroOuNegativos_SaoDescartados()
    {
        RawCatalog raw = Raw.Catalog(Kind.Car, brands: [new RawBrand(0, "Zero"), new RawBrand(-3, "Negativa"), new RawBrand(1, "Honda")]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        Assert.AreEqual("Honda", result.Data.Brands.Single().Name);
        CollectionAssert.AreEquivalent(new[] { "marca|0|id inválido", "marca|-3|id inválido" }, result.Orphans.Select(o => $"{o.Table}|{o.Key}|{o.Reason}").ToArray());
    }

    [TestMethod]
    public void Ano_ForaDeMil950A2100_OuNaoNumerico_ENaoEntra_ComMotivoClaro()
    {
        RawCatalog raw = Raw.Catalog(
            Kind.Car,
            brands: [new RawBrand(1, "Honda")],
            models: [new RawModel(1, 1, "Civic")],
            years: [new RawYear(1, 1, 1949), new RawYear(2, 1, 1950), new RawYear(3, 1, 2100), new RawYear(4, 1, 2101), new RawYear(5, 1, null)],
            versions: [new RawVersion(1, 1, 1949, "Ano fora"), new RawVersion(2, 1, 2100, "Ano no limite")]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        CollectionAssert.AreEqual(new[] { 1950, 2100 }, result.Data.Years.Select(y => y.Year).OrderBy(y => y).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { "ano|1/1949|ano fora de 1950 a 2100", "ano|1/2101|ano fora de 1950 a 2100", "ano|1/|ano inválido", "versão|1|sem ano" },
            result.Orphans.Select(o => $"{o.Table}|{o.Key}|{o.Reason}").ToArray());
        Assert.AreEqual("Ano no limite", result.Data.Versions.Single().Name);
    }

    [TestMethod]
    [DataRow("ImÃ³veis")]
    [DataRow("AutomÃ¡tico")]
    [DataRow("Ã‰poca")]
    [DataRow("Â£ Especial")]
    public void NomeComAcentoCorrompidoNaOrigem_EDescartado_ENaoVaiParaOSite(string corrupted)
    {
        RawCatalog raw = Raw.Catalog(
            Kind.Car,
            brands: [new RawBrand(1, corrupted)],
            models: [new RawModel(1, 1, "Modelo")]);

        ValidationResult result = CatalogValidator.Validate([raw]);

        Assert.IsEmpty(result.Data.Brands);
        CollectionAssert.AreEqual(new[] { "marca|1|texto corrompido", "modelo|1|sem marca" }, result.Orphans.Select(o => $"{o.Table}|{o.Key}|{o.Reason}").ToArray());
    }

    [TestMethod]
    [DataRow("Ão")]
    [DataRow("ÂNGULO 2.0")]
    [DataRow("SÃO PAULO EDITION")]
    [DataRow("Água Ação")]
    public void NomeComAcentoLegitimoMaiusculo_NaoEConfundidoComCorrompido(string legitimate)
    {
        ValidationResult result = CatalogValidator.Validate([Raw.Catalog(Kind.Car, brands: [new RawBrand(1, legitimate)])]);

        Assert.AreEqual(legitimate, result.Data.Brands.Single().Name);
        Assert.IsEmpty(result.Orphans);
    }
}

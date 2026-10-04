using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CitiesImport.Tests;

/// <summary>Leitura do JSON oficial do IBGE e a conferência que recusa a carga inteira quando algo está torto.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ReaderAndValidatorTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Le_OFormatoDaMicrorregiao_ODaRegiaoImediata_EOIdEmTexto()
    {
        IReadOnlyList<RawCity> read = IbgeReader.Read(Ibge.List(
            Ibge.City(3509502, "Campinas", "SP"),
            Ibge.City(5002704, "Campo Grande", "MS", immediateRegion: true),
            Ibge.City("3304557", "Rio de Janeiro", "RJ")));

        CollectionAssert.AreEqual(new[] { 3509502, 5002704, 3304557 }, read.Select(c => c.Code!.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "SP", "MS", "RJ" }, read.Select(c => c.Uf).ToArray());
        Assert.AreEqual("Campo Grande", read[1].Name);
    }

    [TestMethod]
    [DataRow("{}")]
    [DataRow("não é json")]
    [DataRow("")]
    [DataRow("[1,2]")]
    [DataRow("\"texto\"")]
    public void ArquivoForaDoFormato_Falha(string json) => Assert.ThrowsExactly<InvalidOperationException>(() => IbgeReader.Read(json));

    [TestMethod]
    public void CamposAusentes_ViramNulo_ENaoEstouram()
    {
        IReadOnlyList<RawCity> read = IbgeReader.Read("""[{"nome":"Sem código"},{"id":"abc","nome":"Id ruim","microrregiao":{"mesorregiao":{"UF":{"sigla":"SP"}}}}]""");

        Assert.IsNull(read[0].Code);
        Assert.IsNull(read[0].Uf);
        Assert.IsNull(read[1].Code);
    }

    private static ValidationResult Validate(params string[] cities) => CityValidator.Validate(IbgeReader.Read(Ibge.List(cities)));

    [TestMethod]
    public void ListaValida_TrazNomeOficialENameSearchDoNormalizer()
    {
        ValidationResult result = Validate(Ibge.City(3550308, "  São Paulo ", "sp"), Ibge.City(3545803, "Santa Bárbara d'Oeste", "SP"));

        Assert.IsTrue(result.IsValid, CityValidator.Report(result));
        Assert.AreEqual("São Paulo", result.Cities[1].Name, "sem espaços nas pontas");
        Assert.AreEqual("sao paulo", result.Cities[1].NameSearch);
        Assert.AreEqual("santa barbara d'oeste", result.Cities[0].NameSearch);
        Assert.AreEqual("SP", result.Cities[1].Uf, "a sigla sai em maiúsculas");
        CollectionAssert.AreEqual(new[] { 3545803, 3550308 }, result.Cities.Select(c => c.Code).ToArray(), "ordenado por código");
    }

    [TestMethod]
    public void Problemas_RecusamACargaInteira_EDizemOQue()
    {
        ValidationResult result = Validate(
            Ibge.City(3550308, "São Paulo", "SP"),
            Ibge.City(12345, "Código curto", "SP"),
            Ibge.City(35999999, "Código longo", "SP"),
            Ibge.City(3500001, "UF inexistente", "XX"),
            Ibge.City(3300001, "UF trocada", "SP"),
            Ibge.City(3500002, "", "SP"),
            Ibge.City(3500003, new string('x', 81), "SP"));

        Assert.IsFalse(result.IsValid);
        Assert.IsGreaterThanOrEqualTo(6, result.Problems.Count);
        string report = CityValidator.Report(result);
        StringAssert.Contains(report, "Carga recusada");
        StringAssert.Contains(report, "UF 'XX'");
        StringAssert.Contains(report, "dois primeiros dígitos");
        StringAssert.Contains(report, "7 dígitos");
    }

    [TestMethod]
    public void CodigoRepetido_ENomeRepetidoNaMesmaUf_SaoRecusados()
    {
        ValidationResult sameCode = Validate(Ibge.City(3550308, "São Paulo", "SP"), Ibge.City(3550308, "Outro nome", "SP"));
        ValidationResult sameName = Validate(Ibge.City(3550308, "São Paulo", "SP"), Ibge.City(3509502, "SAO PAULO", "SP"));
        ValidationResult otherUf = Validate(Ibge.City(3550308, "Bom Jesus", "SP"), Ibge.City(3300100, "Bom Jesus", "RJ"));

        Assert.IsFalse(sameCode.IsValid);
        StringAssert.Contains(CityValidator.Report(sameCode), "repetido");
        Assert.IsFalse(sameName.IsValid, "mesmo nome, sem acento nem caixa, na mesma UF");
        Assert.IsTrue(otherUf.IsValid, "o mesmo nome em UFs diferentes é normal (Bom Jesus existe em vários estados)");
    }

    [TestMethod]
    public void ListaVazia_EhRecusada() => Assert.IsFalse(CityValidator.Validate([]).IsValid);

    [TestMethod]
    public void AmostraDoRepositorio_EhValida_Com40MunicipiosECodigosCoerentesComAUf()
    {
        ValidationResult result = CityValidator.Validate(IbgeReader.Read(System.IO.File.ReadAllText(Repo.Path("db", "seed", "sample", "cities-sample.json"))));

        Assert.IsTrue(result.IsValid, CityValidator.Report(result));
        Assert.HasCount(40, result.Cities);
        Assert.IsTrue(result.Cities.Any(c => c.Name == "Santa Bárbara d'Oeste"), "a amostra exercita o apóstrofo");
    }
}

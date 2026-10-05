using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Review;

/// <summary>As características do anúncio como o visitante as lê (4.1): uma linha por campo preenchido, na ordem do grupo, com o valor já formatado.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdSpecsTests
#pragma warning restore CA1515
{
    private static FieldGroup Group(string key) => FieldGroupRegistry.Get(key);

    private static string[] Lines(IReadOnlyList<AdSpec> specs) => [.. specs.Select(s => s.Label + ": " + s.Value)];

    [TestMethod]
    public void Carros_UsaOsNomesDoCatalogo_ComQuilometragemEmKmEMilhar()
    {
        AdAttributes attributes = new AdAttributes().Set("brandId", 1).Set("modelId", 11).Set("modelYear", 2019).Set("versionId", 100).Set("km", 45000);
        Dictionary<string, string> labels = new() { ["brandId"] = "Honda", ["modelId"] = "Civic", ["modelYear"] = "2019", ["versionId"] = "LX" };

        string[] lines = Lines(AdSpecs.Build(attributes, Group(FieldGroupKeys.Cars), 33, labels));

        CollectionAssert.AreEqual(new[] { "Marca: Honda", "Modelo: Civic", "Ano: 2019", "Versão: LX", "Quilometragem: 45.000 km" }, lines);
    }

    [TestMethod]
    public void NomeDoCatalogoQueNaoVeio_SomeEmVezDeMostrarOId()
    {
        AdAttributes attributes = new AdAttributes().Set("brandId", 1).Set("modelId", 11).Set("km", 0);

        string[] lines = Lines(AdSpecs.Build(attributes, Group(FieldGroupKeys.Cars), 33, new Dictionary<string, string> { ["brandId"] = "Honda" }));

        CollectionAssert.AreEqual(new[] { "Marca: Honda", "Quilometragem: 0 km" }, lines, "zero é um valor; o modelo sem nome não aparece");
    }

    [TestMethod]
    public void Imoveis_OpcaoDeListaNumeroDinheiroDecimalEMultiplaEscolha()
    {
        AdAttributes attributes = new AdAttributes()
            .Set("propertyTypeId", 1).Set("transactionTypeId", 1).Set("bedrooms", 0).Set("areaM2", 450.75m).Set("condoFeeCents", 85_000L).Set("propertyTaxCents", 123_456L);
        FieldGroup group = Group(FieldGroupKeys.RealEstate);

        string[] lines = Lines(AdSpecs.Build(attributes, group, 26));

        Assert.AreEqual("Quartos: 0", lines.Single(l => l.StartsWith("Quartos", System.StringComparison.Ordinal)), "kitnet (0) é um valor");
        Assert.AreEqual("Área (m²): 450,75", lines.Single(l => l.StartsWith("Área", System.StringComparison.Ordinal)));
        Assert.AreEqual("Condomínio: R$ 850", lines.Single(l => l.StartsWith("Condomínio", System.StringComparison.Ordinal)));
        Assert.AreEqual("IPTU: R$ 1.234,56", lines.Single(l => l.StartsWith("IPTU", System.StringComparison.Ordinal)));
        FieldOption type = group.Field("propertyTypeId").OptionsFor(26).Find(1);
        Assert.AreEqual("Tipo: " + type.Label, lines.Single(l => l.StartsWith("Tipo", System.StringComparison.Ordinal)));
    }

    [TestMethod]
    public void AreaInteira_SemCasasDecimaisSobrando()
    {
        string[] lines = Lines(AdSpecs.Build(new AdAttributes().Set("areaM2", 300m), Group(FieldGroupKeys.RealEstate), 30));

        CollectionAssert.AreEqual(new[] { "Área (m²): 300" }, lines);
    }

    [TestMethod]
    public void MultiplaEscolha_JuntaOsRotulosNaOrdemGravada_EIgnoraIdsDesconhecidos()
    {
        FieldGroup group = Group(FieldGroupKeys.RealEstate);
        FieldList list = group.Field("propertyFeatureIds").OptionsFor(26);
        int first = list.Options[0].Id;
        int second = list.Options[1].Id;

        string[] lines = Lines(AdSpecs.Build(new AdAttributes().Set("propertyFeatureIds", new[] { second, 999_999, first }), group, 26));

        CollectionAssert.AreEqual(new[] { "Características: " + list.Find(second).Label + ", " + list.Find(first).Label }, lines);
    }

    [TestMethod]
    public void CampoVazioOuDeOutraCategoria_NaoAparece()
    {
        FieldGroup group = Group(FieldGroupKeys.RealEstate);
        AdAttributes attributes = new AdAttributes().Set("bedrooms", 3).Set("condoFeeCents", 1000L).Set("propertyFeatureIds", System.Array.Empty<int>());

        string[] terreno = Lines(AdSpecs.Build(attributes, group, 30));

        CollectionAssert.AreEqual(new[] { "Condomínio: R$ 10" }, terreno, "Quartos não existe em Terrenos e a lista vazia não vira linha");
        Assert.IsEmpty(AdSpecs.Build(new AdAttributes(), group, 26));
    }

    [TestMethod]
    public void AnoDoModelo_1950_ViraUmaOuAnterior_ETextoLivreEAparado()
    {
        FieldGroup trucks = Group(FieldGroupKeys.TrucksAndBuses);
        FieldGroup phones = Group(FieldGroupKeys.Phones);

        CollectionAssert.Contains(Lines(AdSpecs.Build(new AdAttributes().Set("modelYear", 1950), trucks, 34)), "Ano do modelo: 1950 ou anterior");
        CollectionAssert.Contains(Lines(AdSpecs.Build(new AdAttributes().Set("modelYear", 2015), trucks, 34)), "Ano do modelo: 2015");
        CollectionAssert.Contains(Lines(AdSpecs.Build(new AdAttributes().Set("model", "  Galaxy S21 "), phones, 94)), "Modelo: Galaxy S21");
    }

    [TestMethod]
    public void HorasDeUso_LevamAUnidade()
    {
        string[] lines = Lines(AdSpecs.Build(new AdAttributes().Set("hoursOfUse", 1200), Group(FieldGroupKeys.BoatsAndAircraft), 40));

        CollectionAssert.Contains(lines, "Horas de uso: 1.200 h");
    }

    [TestMethod]
    public void TipoDeValorErrado_NoJson_EAusente_NuncaUmValorAdivinhado()
    {
        // km gravado como texto e bedrooms como decimal: a leitura é estrita, então as linhas somem
        AdAttributes attributes = new AdAttributes().Set("km", "45000").Set("bedrooms", 2.5m);

        Assert.IsEmpty(AdSpecs.Build(attributes, Group(FieldGroupKeys.Cars), 33));
        Assert.IsEmpty(AdSpecs.Build(attributes, Group(FieldGroupKeys.RealEstate), 26));
    }

    [TestMethod]
    public void CadaLinha_GuardaAChaveDoCampo_EAMultiplaEscolhaOsItensUmAUm()
    {
        FieldGroup jobs = Group(FieldGroupKeys.Jobs);
        FieldList list = jobs.Field(FieldKeys.JobAreas).OptionsFor(96);

        IReadOnlyList<AdSpec> specs = AdSpecs.Build(new AdAttributes().Set(FieldKeys.JobAreas, new[] { 2, 1 }), jobs, 96);

        AdSpec areas = specs.Single(s => s.Key == FieldKeys.JobAreas);
        CollectionAssert.AreEqual(new[] { list.Find(2).Label, list.Find(1).Label }, areas.Items.ToArray(), "os itens vêm separados, sem depender de cortar o texto pela vírgula");
        Assert.AreEqual(string.Join(", ", areas.Items), areas.Value);
        Assert.IsNull(AdSpecs.Build(new AdAttributes().Set("km", 10), Group(FieldGroupKeys.Cars), 33).Single().Items, "só a múltipla escolha tem itens");
        Assert.AreEqual("km", AdSpecs.Build(new AdAttributes().Set("km", 10), Group(FieldGroupKeys.Cars), 33).Single().Key);
    }
}

using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>As pendências para enviar à revisão (US-009) como regra pura: o que cada grupo exige, o texto de cada campo obrigatório e a ordem da lista.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PendingTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Apartments = 26;
    private const int Land = 30;

    /// <summary>Todas as frases de campo obrigatório, como foram revisadas (grupo|campo → frase). O teste confere que não falta nem sobra nenhum campo obrigatório.</summary>
    private static readonly Dictionary<string, string> ExpectedMessages = new()
    {
        ["Services|serviceTypeId"] = "Informe o tipo de serviço",
        ["GeneralProducts|conditionId"] = "Informe a condição",
        ["RealEstate|propertyTypeId"] = "Informe o tipo do imóvel",
        ["RealEstate|transactionTypeId"] = "Informe se o imóvel é para vender ou alugar",
        ["RealEstate|bedrooms"] = "Informe o número de quartos",
        ["RealEstate|areaM2"] = "Informe a área",
        ["Cars|brandId"] = "Informe a marca",
        ["Cars|modelId"] = "Informe o modelo",
        ["Cars|modelYear"] = "Informe o ano",
        ["Cars|versionId"] = "Informe a versão",
        ["Cars|km"] = "Informe a quilometragem",
        ["Motorcycles|brandId"] = "Informe a marca",
        ["Motorcycles|modelId"] = "Informe o modelo",
        ["Motorcycles|modelYear"] = "Informe o ano",
        ["Motorcycles|versionId"] = "Informe a versão",
        ["Motorcycles|km"] = "Informe a quilometragem",
        ["Motorcycles|displacementId"] = "Informe a cilindrada",
        ["TrucksAndBuses|modelYear"] = "Informe o ano do modelo",
        ["TrucksAndBuses|km"] = "Informe a quilometragem",
        ["BoatsAndAircraft|modelYear"] = "Informe o ano do modelo",
        ["BoatsAndAircraft|hoursOfUse"] = "Informe as horas de uso",
        ["BoatsAndAircraft|vehicleTypeId"] = "Informe o tipo",
        ["Parts|conditionId"] = "Informe a condição",
        ["Seasonal|seasonalTypeId"] = "Informe o tipo de hospedagem",
        ["Seasonal|bedrooms"] = "Informe o número de quartos",
        ["Seasonal|guests"] = "Informe quantas pessoas o imóvel acomoda",
        ["Phones|brandId"] = "Informe a marca",
        ["Phones|model"] = "Informe o modelo",
        ["Phones|conditionId"] = "Informe a condição",
        ["Smartwatches|brandId"] = "Informe a marca",
        ["Smartwatches|conditionId"] = "Informe a condição",
        ["TelephonyProducts|typeId"] = "Informe o tipo",
        ["TelephonyProducts|conditionId"] = "Informe a condição",
        ["Appliances|typeId"] = "Informe o tipo",
        ["Appliances|brand"] = "Informe a marca",
        ["Appliances|voltageId"] = "Informe a voltagem",
        ["Appliances|conditionId"] = "Informe a condição",
        ["ElectronicsAndComputers|conditionId"] = "Informe a condição",
        ["ElectronicsAndComputers|brand"] = "Informe a marca",
        ["ClothingAndShoes|conditionId"] = "Informe a condição",
        ["ClothingAndShoes|sizeId"] = "Informe o tamanho",
        ["Machinery|conditionId"] = "Informe a condição"
    };

    private static Ad Complete(int? categoryId = null, AdAttributes attributes = null, long? price = 5_000, string description = "Descrição", string cep = "13015100", string city = "Campinas", string uf = "SP")
    {
        Ad ad = Ad.CreateDraft("Título", 1);
        ad.SetText("Título", description);
        ad.SetCategory(categoryId);
        ad.SetPrice(price);
        ad.SetLocation(cep, city, uf, false);
        ad.SetAttributes(attributes ?? new AdAttributes());
        return ad;
    }

    private static string[] Messages(Ad ad, FieldGroup group, int photos) => [.. AdSubmissionRules.Pending(ad, group, photos).Select(p => p.Message)];

    [TestMethod]
    public void RascunhoCompleto_NaoTemPendencias()
    {
        FieldGroup group = FieldGroupRegistry.Get(FieldGroupKeys.GeneralProducts);

        Assert.IsEmpty(AdSubmissionRules.Pending(Complete(86, new AdAttributes().Set("conditionId", 2)), group, 1));
    }

    [TestMethod]
    public void Vagas_NaoExigeFoto_Servicos_NaoExigePreco()
    {
        Ad job = Complete(96);
        Ad service = Complete(66, new AdAttributes().Set("serviceTypeId", 1), price: null);

        Assert.IsEmpty(AdSubmissionRules.Pending(job, FieldGroupRegistry.Get(FieldGroupKeys.Jobs), 0), "vagas de emprego não têm fotos");
        Assert.IsEmpty(AdSubmissionRules.Pending(service, FieldGroupRegistry.Get(FieldGroupKeys.Services), 1), "serviços não têm preço");
        CollectionAssert.AreEqual(new[] { "Informe o salário" }, Messages(Complete(96, price: null), FieldGroupRegistry.Get(FieldGroupKeys.Jobs), 0), "vagas exigem o salário, com o nome do campo");
        CollectionAssert.AreEqual(new[] { "Adicione ao menos 1 foto" }, Messages(service, FieldGroupRegistry.Get(FieldGroupKeys.Services), 0), "serviços exigem foto");
    }

    [TestMethod]
    public void Carros_ExigemMarcaModeloAnoVersaoEQuilometragem()
    {
        FieldGroup group = FieldGroupRegistry.Get(FieldGroupKeys.Cars);

        string[] messages = Messages(Complete(Cars), group, 1);

        CollectionAssert.AreEqual(
            new[] { "Informe a marca", "Informe o modelo", "Informe o ano", "Informe a versão", "Informe a quilometragem" }, messages,
            "a ordem é a do formulário");
        Assert.IsEmpty(AdSubmissionRules.Pending(
            Complete(Cars, new AdAttributes().Set("brandId", 1).Set("modelId", 11).Set("modelYear", 2019).Set("versionId", 100).Set("km", 0)), group, 1),
            "quilometragem 0 (carro zero) conta como preenchida");
    }

    [TestMethod]
    public void Terrenos_ExigemTipoVenderOuAlugarEArea_ApartamentosExigemTambemQuartosMasNaoArea()
    {
        FieldGroup group = FieldGroupRegistry.Get(FieldGroupKeys.RealEstate);

        string[] land = Messages(Complete(Land), group, 1);
        string[] apartment = Messages(Complete(Apartments), group, 1);

        CollectionAssert.AreEqual(new[] { "Informe o tipo do imóvel", "Informe se o imóvel é para vender ou alugar", "Informe a área" }, land, "Quartos só existe em apartamentos e casas; a área é obrigatória só em Terrenos");
        CollectionAssert.AreEqual(new[] { "Informe o tipo do imóvel", "Informe se o imóvel é para vender ou alugar", "Informe o número de quartos" }, apartment);
        foreach (int category in new[] { Apartments, 27, 31 })
        {
            CollectionAssert.DoesNotContain(Messages(Complete(category), group, 1), "Informe a área", $"categoria {category}: a área segue opcional");
        }

        Assert.IsEmpty(AdSubmissionRules.Pending(
            Complete(Land, new AdAttributes().Set("propertyTypeId", 1).Set("transactionTypeId", 1).Set("areaM2", 450.75m)), group, 1),
            "terreno completo com área não tem pendência");
        Assert.IsEmpty(AdSubmissionRules.Pending(
            Complete(Apartments, new AdAttributes().Set("propertyTypeId", 1).Set("transactionTypeId", 1).Set("bedrooms", 0)), group, 1),
            "zero quartos (kitnet) conta como preenchido");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("\n \t")]
    public void DescricaoVaziaOuSoComEspacos_Impede(string description)
    {
        FieldGroup group = FieldGroupRegistry.Get(FieldGroupKeys.GeneralProducts);

        CollectionAssert.AreEqual(new[] { "Informe a descrição" }, Messages(Complete(86, new AdAttributes().Set("conditionId", 2), description: description), group, 1));
    }

    [TestMethod]
    public void NasCategoriasComInformacoesAdicionais_APendenciaUsaONomeDoCampo()
    {
        Ad service = Complete(66, new AdAttributes().Set("serviceTypeId", 1), description: " ");

        CollectionAssert.AreEqual(new[] { "Informe as informações adicionais" }, Messages(service, FieldGroupRegistry.Get(FieldGroupKeys.Services), 1));
    }

    [TestMethod]
    public void SemPreco_Impede_EZeroNemChegaAoBanco()
    {
        FieldGroup group = FieldGroupRegistry.Get(FieldGroupKeys.GeneralProducts);

        CollectionAssert.AreEqual(new[] { "Informe o preço" }, Messages(Complete(86, new AdAttributes().Set("conditionId", 2), price: null), group, 1));
        Assert.ThrowsExactly<GazetaMarketplace.Core.Exceptions.ValidationException>(() => Complete(86, price: 0), "o anúncio nunca guarda preço zero (S28), então a regra de envio só vê vazio");
    }

    [TestMethod]
    public void Cep_SemCepOuInvalido_PedeOCep_ComCepEmCidade_PedeACidade()
    {
        FieldGroup group = FieldGroupRegistry.Get(FieldGroupKeys.GeneralProducts);
        AdAttributes attributes = new AdAttributes().Set("conditionId", 2);

        CollectionAssert.AreEqual(new[] { "Informe o CEP" }, Messages(Complete(86, attributes, cep: null, city: null, uf: null), group, 1));
        CollectionAssert.AreEqual(new[] { "Informe a cidade" }, Messages(Complete(86, attributes, cep: "13015100", city: "", uf: "SP"), group, 1));
        CollectionAssert.AreEqual(new[] { "Informe a cidade" }, Messages(Complete(86, attributes, cep: "13015100", city: "Campinas", uf: " "), group, 1));
    }

    [TestMethod]
    public void SemCategoria_PedeACategoria_ENaoExigeCamposDeGrupo()
    {
        FieldGroup group = FieldGroupRegistry.Default;

        CollectionAssert.AreEqual(new[] { "Escolha uma categoria" }, Messages(Complete(), group, 1));
    }

    [TestMethod]
    public void AOrdemDaListaEADaTela()
    {
        Ad ad = Ad.CreateDraft("x", 1);
        ad.SetText("x", null);
        ad.SetCategory(Cars);
        FieldGroup group = FieldGroupRegistry.Get(FieldGroupKeys.Cars);

        IReadOnlyList<AdPending> pending = AdSubmissionRules.Pending(ad, group, 0);

        CollectionAssert.AreEqual(
            new[] { "Informe a descrição", "Informe o preço", "Informe o CEP", "Adicione ao menos 1 foto", "Informe a marca", "Informe o modelo", "Informe o ano", "Informe a versão", "Informe a quilometragem" },
            pending.Select(p => p.Message).ToArray());
        CollectionAssert.AreEqual(
            new[] { "descricao", "preco", "cep", "fotos", "campo-brandId", "campo-modelId", "campo-modelYear", "campo-versionId", "campo-km" },
            pending.Select(p => p.Target).ToArray(), "cada pendência aponta para o id do campo na tela");
    }

    [TestMethod]
    public void TodoCampoObrigatorioTemFrasePropria_ComOArtigoCerto_ENenhumFicaDeFora()
    {
        Dictionary<string, string> actual = [];
        foreach (FieldGroup group in FieldGroupRegistry.All)
        {
            foreach (FieldDefinition field in group.Fields.Where(f => f.Required || f.RequiredForCategories is not null))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(field.RequiredMessage), $"{group.Key}|{field.Key} é obrigatório e não tem frase");
                actual[$"{group.Key}|{field.Key}"] = field.RequiredMessage;
            }
        }

        CollectionAssert.AreEquivalent(ExpectedMessages.Keys.ToList(), actual.Keys.ToList(), "o conjunto de campos obrigatórios mudou: revise as frases");
        foreach ((string key, string expected) in ExpectedMessages)
        {
            Assert.AreEqual(expected, actual[key], key);
        }

        Assert.IsTrue(actual.Values.All(m => m.StartsWith("Informe ", System.StringComparison.Ordinal)), "todas começam com \"Informe\"");
    }

    [TestMethod]
    public void CampoObrigatorioSemFrase_CaiNoRotuloEmVezDeFicarMudo()
    {
        FieldGroup group = new()
        {
            Key = "teste",
            Name = "Teste",
            Fields = [new FieldDefinition { Key = "cor", Label = "Cor", Type = FieldType.Text, Required = true }]
        };

        CollectionAssert.AreEqual(new[] { "Informe: Cor" }, Messages(Complete(999), group, 1));
    }
}

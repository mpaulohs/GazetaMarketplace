using System.Linq;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>Os quatro primeiros grupos (tarefa 2.2) contra o Apêndice B da SPEC: limites, campos, ordem e obrigatórios.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class GroupsTests
#pragma warning restore CA1515
{
    private static FieldGroup Group(string key) => FieldGroupRegistry.Get(key)!;

    [TestMethod]
    public void Servicos_SemPreco_6Fotos_Tipo11Opcoes()
    {
        FieldGroup services = Group(FieldGroupKeys.Services);

        Assert.IsFalse(services.HasPrice);
        Assert.IsNull(services.PriceLabel);
        Assert.AreEqual(6, services.MaxPhotos);
        Assert.AreEqual(1, services.MinPhotosToSubmit);
        Assert.AreEqual(120, services.TitleMaxLength);
        Assert.AreEqual(6000, services.DescriptionMaxLength);
        Assert.AreEqual("Informações adicionais", services.DescriptionLabel);

        FieldDefinition type = services.Fields.Single();
        Assert.AreEqual("serviceTypeId", type.Key);
        Assert.AreEqual("Tipo", type.Label);
        Assert.AreEqual(FieldType.Select, type.Type);
        Assert.IsTrue(type.Required);
        Assert.HasCount(11, type.OptionsFor(66).Options);
    }

    [TestMethod]
    public void Vagas_SemFotos_Titulo90_14Areas_PrecoViraSalario()
    {
        FieldGroup jobs = Group(FieldGroupKeys.Jobs);

        Assert.AreEqual(0, jobs.MaxPhotos);
        Assert.AreEqual(0, jobs.MinPhotosToSubmit, "sem fotos, nenhuma é exigida para enviar");
        Assert.AreEqual(90, jobs.TitleMaxLength);
        Assert.AreEqual(6000, jobs.DescriptionMaxLength);
        Assert.AreEqual("Informações adicionais", jobs.DescriptionLabel);
        Assert.IsTrue(jobs.HasPrice);
        Assert.AreEqual("Salário", jobs.PriceLabel);
        StringAssert.StartsWith(jobs.TitleHelp, "Sugerimos especificar a vaga com clareza");
        StringAssert.StartsWith(jobs.DescriptionPlaceholder, "Escreva aqui por que você está anunciando esta vaga");
        StringAssert.StartsWith(jobs.DescriptionHelp, "Inclua detalhes sobre o cargo");

        FieldDefinition area = jobs.Fields.Single();
        Assert.AreEqual("jobAreaIds", area.Key);
        Assert.AreEqual(FieldType.MultiSelect, area.Type);
        Assert.IsFalse(area.Required, "Área é opcional");
        Assert.HasCount(14, area.OptionsFor(96).Options);
    }

    [TestMethod]
    public void ProdutosEmGeral_CondicaoObrigatoria_5Opcoes_TipoDeProdutoOpcionalEmTextoLivre()
    {
        FieldGroup general = Group(FieldGroupKeys.GeneralProducts);

        Assert.AreSame(general, FieldGroupRegistry.Default);
        Assert.AreEqual(20, general.MaxPhotos);
        Assert.AreEqual(1, general.MinPhotosToSubmit);
        Assert.AreEqual(120, general.TitleMaxLength);
        Assert.AreEqual(5000, general.DescriptionMaxLength);
        Assert.AreEqual("Descrição", general.DescriptionLabel);
        Assert.AreEqual("Preço", general.PriceLabel);

        FieldDefinition condition = general.Field("conditionId")!;
        Assert.IsTrue(condition.Required);
        Assert.HasCount(5, condition.OptionsFor(135).Options);

        FieldDefinition productType = general.Field("productType")!;
        Assert.AreEqual(FieldType.Text, productType.Type);
        Assert.IsFalse(productType.Required);
        Assert.AreEqual(60, productType.MaxLength);
        Assert.AreEqual(SuggestionSource.ProductTypes, productType.Suggestions);
        Assert.IsEmpty(general.FilterableFieldsFor(135), "nenhum filtro específico em Produtos em geral (A7 c)");
        CollectionAssert.AreEqual(new[] { "conditionId", "productType" }, general.Fields.Select(f => f.Key).ToArray());
    }

    [TestMethod]
    public void Imoveis_ObrigatoriosPorCategoria()
    {
        FieldGroup realEstate = Group(FieldGroupKeys.RealEstate);

        CollectionAssert.AreEqual(new[] { "propertyTypeId", "transactionTypeId", "bedrooms" }, realEstate.RequiredFieldsFor(26).Select(f => f.Key).ToArray(), "Apartamentos");
        CollectionAssert.AreEqual(new[] { "propertyTypeId", "transactionTypeId", "bedrooms" }, realEstate.RequiredFieldsFor(27).Select(f => f.Key).ToArray(), "Casas");
        CollectionAssert.AreEqual(new[] { "propertyTypeId", "transactionTypeId" }, realEstate.RequiredFieldsFor(30).Select(f => f.Key).ToArray(), "Terrenos: sem quartos");
        CollectionAssert.AreEqual(new[] { "propertyTypeId", "transactionTypeId" }, realEstate.RequiredFieldsFor(31).Select(f => f.Key).ToArray(), "Comércio: sem quartos");
    }

    [TestMethod]
    public void Imoveis_CamposQueExistemEmCadaCategoria()
    {
        FieldGroup realEstate = Group(FieldGroupKeys.RealEstate);
        string[] Keys(int id) => [.. realEstate.FieldsFor(id).Select(f => f.Key)];

        CollectionAssert.AreEqual(
            new[] { "propertyTypeId", "transactionTypeId", "bedrooms", "bathrooms", "areaM2", "parkingSpaces", "condoFeeCents", "propertyTaxCents", "propertyFeatureIds", "condoFeatureIds" },
            Keys(26));
        CollectionAssert.AreEqual(Keys(26), Keys(27));
        CollectionAssert.AreEqual(
            new[] { "propertyTypeId", "transactionTypeId", "areaM2", "condoFeeCents", "propertyTaxCents", "propertyFeatureIds" }, Keys(30), "Terrenos: sem quartos, banheiros, vagas nem condomínio");
        CollectionAssert.AreEqual(
            new[] { "propertyTypeId", "transactionTypeId", "areaM2", "parkingSpaces", "condoFeeCents", "propertyTaxCents", "propertyFeatureIds" }, Keys(31), "Comércio: com vagas");
    }

    [TestMethod]
    public void Imoveis_ListasMudamPorCategoria()
    {
        FieldGroup realEstate = Group(FieldGroupKeys.RealEstate);
        FieldDefinition type = realEstate.Field("propertyTypeId")!;
        FieldDefinition features = realEstate.Field("propertyFeatureIds")!;
        FieldDefinition condo = realEstate.Field("condoFeatureIds")!;

        Assert.AreSame(FieldLists.ApartmentType, type.OptionsFor(26));
        Assert.AreSame(FieldLists.HouseType, type.OptionsFor(27));
        Assert.AreSame(FieldLists.LandType, type.OptionsFor(30));
        Assert.AreSame(FieldLists.CommercialType, type.OptionsFor(31));
        Assert.AreSame(FieldLists.ApartmentFeature, features.OptionsFor(26));
        Assert.AreSame(FieldLists.ApartmentFeature, features.OptionsFor(27), "Casas reaproveita as características de Apartamentos");
        Assert.AreSame(FieldLists.LandFeature, features.OptionsFor(30));
        Assert.AreSame(FieldLists.CommercialFeature, features.OptionsFor(31));
        Assert.AreSame(FieldLists.ApartmentCondoFeature, condo.OptionsFor(26));
        Assert.AreSame(FieldLists.HouseCondoFeature, condo.OptionsFor(27));
        Assert.AreSame(FieldLists.PropertyTransactionType, realEstate.Field("transactionTypeId")!.OptionsFor(30));
    }

    [TestMethod]
    public void Imoveis_LimitesNumericos_AreaDecimal_DinheiroEmCentavos()
    {
        FieldGroup realEstate = Group(FieldGroupKeys.RealEstate);

        foreach (string key in new[] { "bedrooms", "bathrooms", "parkingSpaces" })
        {
            FieldDefinition count = realEstate.Field(key)!;
            Assert.AreEqual(FieldType.Integer, count.Type, key);
            Assert.AreEqual(0m, count.Min, key);
            Assert.AreEqual(20m, count.Max, key);
        }

        FieldDefinition area = realEstate.Field("areaM2")!;
        Assert.AreEqual(FieldType.Decimal, area.Type);
        Assert.AreEqual(2, area.DecimalPlaces);
        Assert.AreEqual(FieldFilter.Range, area.Filter, "filtro de faixa por área (A7 c)");
        Assert.IsFalse(area.Required);

        foreach (string key in new[] { "condoFeeCents", "propertyTaxCents" })
        {
            FieldDefinition money = realEstate.Field(key)!;
            Assert.AreEqual(FieldType.Money, money.Type, key);
            Assert.AreEqual(9_999_999_999m, money.Max, "mesmo teto do preço: R$ 99.999.999,99 em centavos");
            Assert.IsFalse(money.Required);
        }

        CollectionAssert.AreEqual(new[] { "areaM2" }, realEstate.FilterableFieldsFor(26).Select(f => f.Key).ToArray());
        CollectionAssert.AreEqual(new[] { "areaM2" }, realEstate.FilterableFieldsFor(30).Select(f => f.Key).ToArray());
    }

    [TestMethod]
    public void TodoGrupo_TemChavesUnicasDeCampo_ELimitesCoerentes()
    {
        foreach (FieldGroup group in FieldGroupRegistry.All)
        {
            string[] keys = [.. group.Fields.Select(f => f.Key)];
            Assert.AreEqual(keys.Length, keys.Distinct().Count(), $"chaves repetidas em {group.Key}");
            Assert.IsGreaterThanOrEqualTo(0, group.MaxPhotos, group.Key);
            Assert.IsLessThanOrEqualTo(group.MaxPhotos, group.MinPhotosToSubmit, $"mínimo de fotos acima do máximo em {group.Key}");
            Assert.IsLessThanOrEqualTo(120, group.TitleMaxLength, $"o título cabe na coluna Ads.Title(120): {group.Key}");
            Assert.IsGreaterThan(0, group.TitleMaxLength);
            Assert.IsGreaterThan(0, group.DescriptionMaxLength);
            Assert.AreEqual(group.HasPrice, group.PriceLabel is not null, $"rótulo de preço só existe se há preço: {group.Key}");

            foreach (FieldDefinition field in group.Fields)
            {
                bool isList = field.Type is FieldType.Select or FieldType.MultiSelect;
                Assert.AreEqual(isList, field.Options is not null || field.OptionsByCategory is not null, $"{group.Key}.{field.Key}: só campo de lista tem opções");
                Assert.AreEqual(field.Type == FieldType.CatalogItem, field.Catalog is not null, $"{group.Key}.{field.Key}: só campo de catálogo tem referência ao catálogo");
                if (field.Type == FieldType.Text)
                {
                    Assert.IsNotNull(field.MaxLength, $"{group.Key}.{field.Key}: texto precisa de limite");
                }
            }
        }
    }

    [TestMethod]
    public void Grupos_Os18DoApendiceB_ComChavesEstaveis()
    {
        CollectionAssert.AreEquivalent(
            new[]
            {
                "Services", "Jobs", "GeneralProducts", "RealEstate", "Cars", "Motorcycles", "TrucksAndBuses", "BoatsAndAircraft", "Parts",
                "RoomRental", "Seasonal", "Phones", "Smartwatches", "TelephonyProducts", "Appliances", "ElectronicsAndComputers", "ClothingAndShoes", "Machinery"
            },
            FieldGroupRegistry.All.Select(g => g.Key).ToArray());
        Assert.IsNull(FieldGroupRegistry.Get("NaoExiste"));
        Assert.IsNull(FieldGroupRegistry.Get(null));
    }
}

using System.Linq;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>
/// Os nove grupos que fecham o Apêndice B (tarefa 2.4): listas, limites e campos por categoria. Rótulos, ordem e obrigatoriedade já são conferidos
/// contra a SPEC em <see cref="AppendixBTests"/>; aqui ficam as decisões de 2026-10-03 (marca e modelo em texto livre, limites numéricos, tamanhos).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class RemainingGroupsTests
#pragma warning restore CA1515
{
    private static FieldGroup Group(string key) => FieldGroupRegistry.Get(key)!;

    private static string[] Keys(FieldGroup group, int category) => [.. group.FieldsFor(category).Select(f => f.Key)];

    [TestMethod]
    public void AluguelDeQuartos_SoCaracteristicas_VariasOpcoes_SemObrigatorios()
    {
        FieldGroup rooms = Group(FieldGroupKeys.RoomRental);

        FieldDefinition features = rooms.Fields.Single();
        Assert.AreEqual("roomFeatureIds", features.Key);
        Assert.AreEqual(FieldType.MultiSelect, features.Type);
        Assert.AreSame(FieldLists.RoomFeature, features.Options);
        Assert.IsEmpty(rooms.RequiredFieldsFor(28));
        Assert.IsEmpty(rooms.FilterableFieldsFor(28), "sem filtro específico (A7 c)");
    }

    [TestMethod]
    public void Temporada_Obrigatorios_TipoQuartosPessoas_ComLimitesAprovados()
    {
        FieldGroup seasonal = Group(FieldGroupKeys.Seasonal);

        CollectionAssert.AreEqual(new[] { "seasonalTypeId", "bedrooms", "guests" }, seasonal.RequiredFieldsFor(29).Select(f => f.Key).ToArray());
        foreach (string key in new[] { "bedrooms", "bathrooms", "parkingSpaces" })
        {
            FieldDefinition count = seasonal.Field(key)!;
            Assert.AreEqual(FieldType.Integer, count.Type, key);
            Assert.AreEqual(0m, count.Min, $"{key}: 0 é válido (0 quarto = estúdio)");
            Assert.AreEqual(20m, count.Max, key);
        }

        FieldDefinition guests = seasonal.Field("guests")!;
        Assert.AreEqual(1m, guests.Min, "alguém precisa se hospedar");
        Assert.AreEqual(50m, guests.Max);
        Assert.AreSame(FieldLists.SeasonalType, seasonal.Field("seasonalTypeId")!.Options);
        Assert.AreSame(FieldLists.SeasonalPaymentType, seasonal.Field("paymentTypeId")!.Options);
        Assert.AreSame(FieldLists.SeasonalFeature, seasonal.Field("seasonalFeatureIds")!.Options);
        Assert.AreEqual(FieldType.MultiSelect, seasonal.Field("seasonalFeatureIds")!.Type);
    }

    [TestMethod]
    public void Celulares_MarcaEhLista_ModeloEhTextoDe60_CondicaoEhACompartilhada()
    {
        FieldGroup phones = Group(FieldGroupKeys.Phones);

        CollectionAssert.AreEqual(new[] { "brandId", "model", "conditionId" }, phones.RequiredFieldsFor(43).Select(f => f.Key).ToArray());
        Assert.AreSame(FieldLists.PhoneBrand, phones.Field("brandId")!.Options);
        FieldDefinition model = phones.Field("model")!;
        Assert.AreEqual(FieldType.Text, model.Type);
        Assert.AreEqual(60, model.MaxLength);
        Assert.AreSame(FieldLists.ProductCondition, phones.Field("conditionId")!.Options);
        Assert.AreSame(FieldLists.PhoneStorage, phones.Field("storageId")!.Options);
        Assert.AreSame(FieldLists.PhoneColor, phones.Field("colorId")!.Options);
        Assert.AreSame(FieldLists.PhoneBatteryHealth, phones.Field("batteryHealthId")!.Options);
    }

    [TestMethod]
    public void Smartwatches_MarcaPropria_ECondicao()
    {
        FieldGroup watches = Group(FieldGroupKeys.Smartwatches);

        CollectionAssert.AreEqual(new[] { "brandId", "conditionId" }, watches.RequiredFieldsFor(46).Select(f => f.Key).ToArray());
        Assert.AreSame(FieldLists.SmartwatchBrand, watches.Field("brandId")!.Options);
        Assert.AreNotSame(FieldLists.PhoneBrand, watches.Field("brandId")!.Options, "smartwatch tem lista de marcas própria");
    }

    [TestMethod]
    public void ProdutosDeTelefonia_TipoMudaPorCategoria_MarcasCompativeisSoEm44E45()
    {
        FieldGroup telephony = Group(FieldGroupKeys.TelephonyProducts);
        FieldDefinition type = telephony.Field("typeId")!;

        Assert.AreSame(FieldLists.PhoneAccessoryType, type.OptionsFor(44));
        Assert.AreSame(FieldLists.PhonePartType, type.OptionsFor(45));
        Assert.AreSame(FieldLists.SmartwatchAccessoryType, type.OptionsFor(47));
        Assert.AreSame(FieldLists.LandlinePhoneType, type.OptionsFor(48));

        CollectionAssert.AreEqual(new[] { "typeId", "conditionId", "compatibleBrandIds" }, Keys(telephony, 44));
        CollectionAssert.AreEqual(new[] { "typeId", "conditionId", "compatibleBrandIds" }, Keys(telephony, 45));
        CollectionAssert.AreEqual(new[] { "typeId", "conditionId" }, Keys(telephony, 47));
        CollectionAssert.AreEqual(new[] { "typeId", "conditionId" }, Keys(telephony, 48));
        CollectionAssert.AreEqual(new[] { "typeId", "conditionId" }, telephony.RequiredFieldsFor(44).Select(f => f.Key).ToArray(), "Marcas compatíveis é opcional");
        Assert.AreEqual(FieldType.MultiSelect, telephony.Field("compatibleBrandIds")!.Type);
        Assert.AreSame(FieldLists.PhoneBrand, telephony.Field("compatibleBrandIds")!.Options);
    }

    [TestMethod]
    public void Eletro_TipoTemUmaListaPorCategoria_MarcaEhTextoLivreComSugestoes_CapacidadeSoNoArCondicionado()
    {
        FieldGroup appliances = Group(FieldGroupKeys.Appliances);
        FieldDefinition type = appliances.Field("typeId")!;

        FieldList[] expected =
        [
            FieldLists.AirConditionerType, FieldLists.FanType, FieldLists.RefrigeratorType, FieldLists.StoveType,
            FieldLists.WasherType, FieldLists.KitchenApplianceType, FieldLists.PersonalCareApplianceType
        ];
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreSame(expected[i], type.OptionsFor(128 + i), $"tipo da categoria {128 + i}");
        }

        FieldDefinition brand = appliances.Field("brand")!;
        Assert.AreEqual(FieldType.Text, brand.Type);
        Assert.AreEqual(60, brand.MaxLength);
        Assert.AreEqual(SuggestionSource.Brands, brand.Suggestions);
        Assert.IsTrue(brand.Required);

        Assert.AreSame(FieldLists.ApplianceVoltage, appliances.Field("voltageId")!.Options);
        Assert.IsTrue(appliances.Field("voltageId")!.Required);
        Assert.IsTrue(appliances.Field("capacityId")!.AppliesTo(128));
        for (int id = 129; id <= 134; id++)
        {
            Assert.IsFalse(appliances.Field("capacityId")!.AppliesTo(id), $"Capacidade não existe na categoria {id}");
            CollectionAssert.AreEqual(new[] { "typeId", "brand", "voltageId", "conditionId" }, appliances.RequiredFieldsFor(id).Select(f => f.Key).ToArray());
        }

        Assert.AreSame(FieldLists.AirConditionerCapacity, appliances.Field("capacityId")!.Options);
        Assert.IsFalse(appliances.Field("capacityId")!.Required);
    }

    [TestMethod]
    public void EletronicosEInformatica_MarcaObrigatoriaEmTexto60ComSugestoes_ModeloOpcionalEmTexto60()
    {
        FieldGroup electronics = Group(FieldGroupKeys.ElectronicsAndComputers);

        CollectionAssert.AreEqual(new[] { "conditionId", "brand" }, electronics.RequiredFieldsFor(102).Select(f => f.Key).ToArray());
        FieldDefinition brand = electronics.Field("brand")!;
        Assert.AreEqual(60, brand.MaxLength);
        Assert.AreEqual(SuggestionSource.Brands, brand.Suggestions);
        FieldDefinition model = electronics.Field("model")!;
        Assert.AreEqual(FieldType.Text, model.Type);
        Assert.AreEqual(60, model.MaxLength);
        Assert.IsFalse(model.Required);
        Assert.AreEqual(SuggestionSource.None, model.Suggestions);
    }

    [TestMethod]
    public void RoupasECalcados_TamanhoMudaPorCategoria_GeneroEhOMesmoNasOito()
    {
        FieldGroup clothing = Group(FieldGroupKeys.ClothingAndShoes);
        FieldDefinition size = clothing.Field("sizeId")!;

        foreach (int id in new[] { 64, 68, 72, 76 })
        {
            Assert.AreSame(FieldLists.ClothingSize, size.OptionsFor(id), $"roupa {id}");
        }

        foreach (int id in new[] { 65, 69 })
        {
            Assert.AreSame(FieldLists.AdultShoeSize, size.OptionsFor(id), $"calçado adulto {id}");
        }

        foreach (int id in new[] { 75, 77 })
        {
            Assert.AreSame(FieldLists.ChildShoeSize, size.OptionsFor(id), $"calçado infantil ou de bebê {id}");
        }

        FieldDefinition gender = clothing.Field("genderId")!;
        Assert.AreSame(FieldLists.Gender, gender.Options, "uma lista só de gênero para as oito categorias");
        Assert.IsFalse(gender.Required);
        Assert.IsNull(clothing.Field("brand"), "o Apêndice B não pede Marca em Roupas e calçados");
        CollectionAssert.AreEqual(new[] { "conditionId", "sizeId" }, clothing.RequiredFieldsFor(65).Select(f => f.Key).ToArray());
    }

    [TestMethod]
    public void Maquinas_SoACondicaoEhObrigatoria_MarcaTextoLivre_HorasDe0A999999()
    {
        FieldGroup machinery = Group(FieldGroupKeys.Machinery);

        CollectionAssert.AreEqual(new[] { "conditionId" }, machinery.RequiredFieldsFor(89).Select(f => f.Key).ToArray());
        FieldDefinition brand = machinery.Field("brand")!;
        Assert.AreEqual(60, brand.MaxLength);
        Assert.AreEqual(SuggestionSource.Brands, brand.Suggestions);
        Assert.IsFalse(brand.Required);

        FieldDefinition hours = machinery.Field("hoursOfUse")!;
        Assert.AreEqual(FieldType.Integer, hours.Type);
        Assert.AreEqual(0m, hours.Min);
        Assert.AreEqual(999_999m, hours.Max);
        Assert.IsFalse(hours.Required);
    }

    [TestMethod]
    public void NenhumDosNoveGrupos_TemFiltroEspecifico_NemCamposDeCatalogo()
    {
        foreach (string key in new[]
        {
            FieldGroupKeys.RoomRental, FieldGroupKeys.Seasonal, FieldGroupKeys.Phones, FieldGroupKeys.Smartwatches, FieldGroupKeys.TelephonyProducts,
            FieldGroupKeys.Appliances, FieldGroupKeys.ElectronicsAndComputers, FieldGroupKeys.ClothingAndShoes, FieldGroupKeys.Machinery
        })
        {
            FieldGroup group = Group(key);
            Assert.IsTrue(group.Fields.All(f => f.Filter == FieldFilter.None), $"A7 c: filtros específicos só em Imóveis, Carros, Motos, Caminhões e Ônibus ({key})");
            Assert.IsTrue(group.Fields.All(f => f.Type != FieldType.CatalogItem), key);
            Assert.AreEqual(20, group.MaxPhotos, key);
            Assert.IsTrue(group.HasPrice, key);
        }
    }
}

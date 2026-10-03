using System.Linq;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>Carros, Motos, Caminhões e ônibus, Barcos e aeronaves e Peças (tarefa 2.3) contra o Apêndice B da SPEC.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class VehicleGroupsTests
#pragma warning restore CA1515
{
    private static FieldGroup Group(string key) => FieldGroupRegistry.Get(key)!;

    private static string[] Keys(FieldGroup group, int category) => [.. group.FieldsFor(category).Select(f => f.Key)];

    private static string[] RequiredKeys(FieldGroup group, int category) => [.. group.RequiredFieldsFor(category).Select(f => f.Key)];

    [TestMethod]
    public void Carros_Obrigatorios_MarcaModeloAnoVersaoKm()
    {
        FieldGroup cars = Group(FieldGroupKeys.Cars);

        CollectionAssert.AreEqual(new[] { "brandId", "modelId", "modelYear", "versionId", "km" }, RequiredKeys(cars, 33));
        CollectionAssert.AreEqual(
            new[]
            {
                "brandId", "modelId", "modelYear", "versionId", "km", "transmissionId", "doorsId", "fuelId", "steeringId", "vehicleTypeId", "enginePowerId",
                "colorId", "optionalItemIds", "additionalInfoIds"
            },
            Keys(cars, 33),
            "a ordem do Apêndice B: catálogo, km, câmbio, portas, combustível, direção, tipo, potência, cor, opcionais, informações adicionais");
    }

    [TestMethod]
    public void Carros_AsQuatroPartesDoCatalogo_SaoDeCarro_NaOrdemMarcaModeloAnoVersao()
    {
        FieldGroup cars = Group(FieldGroupKeys.Cars);

        FieldDefinition[] chain = [.. cars.Fields.Take(4)];

        Assert.IsTrue(chain.All(f => f.Type == FieldType.CatalogItem && f.Catalog!.Kind == CatalogKind.Car));
        CollectionAssert.AreEqual(
            new[] { CatalogLevel.Brand, CatalogLevel.Model, CatalogLevel.Year, CatalogLevel.Version },
            chain.Select(f => f.Catalog!.Level).ToArray());
    }

    [TestMethod]
    public void Carros_ListasDoGazetaOnline()
    {
        FieldGroup cars = Group(FieldGroupKeys.Cars);

        Assert.AreSame(FieldLists.CarTransmission, cars.Field("transmissionId")!.Options);
        Assert.AreSame(FieldLists.CarDoor, cars.Field("doorsId")!.Options);
        Assert.AreSame(FieldLists.CarFuel, cars.Field("fuelId")!.Options);
        Assert.AreSame(FieldLists.CarSteeringGear, cars.Field("steeringId")!.Options);
        Assert.AreSame(FieldLists.VehicleType, cars.Field("vehicleTypeId")!.Options);
        Assert.AreSame(FieldLists.CarEnginePower, cars.Field("enginePowerId")!.Options);
        Assert.AreSame(FieldLists.CarColor, cars.Field("colorId")!.Options);
        Assert.AreSame(FieldLists.OptionalItem, cars.Field("optionalItemIds")!.Options);
        Assert.AreSame(FieldLists.AdditionalInfo, cars.Field("additionalInfoIds")!.Options);
        Assert.AreEqual(FieldType.MultiSelect, cars.Field("optionalItemIds")!.Type);
        Assert.AreEqual(FieldType.MultiSelect, cars.Field("additionalInfoIds")!.Type);
    }

    [TestMethod]
    public void Motos_Cilindrada_Obrigatoria_ComCatalogoDeMoto()
    {
        FieldGroup motorcycles = Group(FieldGroupKeys.Motorcycles);

        CollectionAssert.AreEqual(new[] { "brandId", "modelId", "modelYear", "versionId", "km", "displacementId" }, RequiredKeys(motorcycles, 36));
        CollectionAssert.AreEqual(
            new[] { "brandId", "modelId", "modelYear", "versionId", "km", "displacementId", "colorId", "optionalItemIds", "additionalInfoIds" },
            Keys(motorcycles, 36));
        Assert.IsTrue(motorcycles.Fields.Take(4).All(f => f.Catalog!.Kind == CatalogKind.Motorcycle));
        Assert.AreSame(FieldLists.MotorcycleDisplacement, motorcycles.Field("displacementId")!.Options);
        Assert.AreSame(FieldLists.CarColor, motorcycles.Field("colorId")!.Options, "Motos usam a lista de cores de carro (GazetaOnline)");
        Assert.AreSame(FieldLists.MotorcycleOptionalItem, motorcycles.Field("optionalItemIds")!.Options);
        Assert.AreSame(FieldLists.MotorcycleAdditionalInfo, motorcycles.Field("additionalInfoIds")!.Options);
    }

    [TestMethod]
    public void CaminhoesOnibus_AnoEKm_Obrigatorios_ListasMudamEntreAsDuas()
    {
        FieldGroup group = Group(FieldGroupKeys.TrucksAndBuses);

        foreach (int category in new[] { 34, 35 })
        {
            CollectionAssert.AreEqual(new[] { "modelYear", "km" }, RequiredKeys(group, category), $"categoria {category}");
            CollectionAssert.AreEqual(
                new[] { "modelYear", "km", "transmissionId", "fuelId", "steeringId", "vehicleTypeId", "optionalItemIds", "additionalInfoIds" },
                Keys(group, category));
        }

        Assert.AreEqual(FieldType.ModelYear, group.Field("modelYear")!.Type, "ano do modelo, não catálogo");
        Assert.AreSame(FieldLists.TruckType, group.Field("vehicleTypeId")!.OptionsFor(34));
        Assert.AreSame(FieldLists.BusType, group.Field("vehicleTypeId")!.OptionsFor(35));
        Assert.AreSame(FieldLists.TruckOptionalItem, group.Field("optionalItemIds")!.OptionsFor(34));
        Assert.AreSame(FieldLists.BusOptionalItem, group.Field("optionalItemIds")!.OptionsFor(35));
        Assert.AreSame(FieldLists.VehicleSteeringGear, group.Field("steeringId")!.Options, "a direção de caminhão e ônibus não é a de carro");
        Assert.AreSame(FieldLists.CarTransmission, group.Field("transmissionId")!.Options);
        Assert.AreSame(FieldLists.VehicleAdditionalInfo, group.Field("additionalInfoIds")!.Options);
    }

    [TestMethod]
    public void Barcos_HorasDeUso_NoLugarDeKm()
    {
        FieldGroup boats = Group(FieldGroupKeys.BoatsAndAircraft);

        CollectionAssert.AreEqual(new[] { "modelYear", "hoursOfUse", "vehicleTypeId" }, RequiredKeys(boats, 37));
        Assert.IsNull(boats.Field("km"), "embarcação e aeronave não medem quilometragem");
        Assert.AreEqual(0m, boats.Field("hoursOfUse")!.Min);
        Assert.AreEqual(999_999m, boats.Field("hoursOfUse")!.Max);
        Assert.AreSame(FieldLists.BoatType, boats.Field("vehicleTypeId")!.Options);
        Assert.AreSame(FieldLists.BoatAdditionalInfo, boats.Field("additionalInfoIds")!.Options);
        Assert.AreSame(FieldLists.CarFuel, boats.Field("fuelId")!.Options);
    }

    [TestMethod]
    public void Barcos_Dimensoes_EmMetrosComDuasCasas_De0_01A999_99()
    {
        FieldGroup boats = Group(FieldGroupKeys.BoatsAndAircraft);

        foreach (string key in new[] { "lengthMeters", "widthMeters", "heightMeters" })
        {
            FieldDefinition meters = boats.Field(key)!;
            Assert.AreEqual(FieldType.Decimal, meters.Type, key);
            Assert.AreEqual(2, meters.DecimalPlaces, key);
            Assert.AreEqual(0.01m, meters.Min, key);
            Assert.AreEqual(999.99m, meters.Max, key);
            Assert.IsFalse(meters.Required, key);
        }
    }

    [TestMethod]
    public void Veiculos_LimiteDeQuilometragem_De0A9999999()
    {
        foreach (FieldGroup group in new[] { Group(FieldGroupKeys.Cars), Group(FieldGroupKeys.Motorcycles), Group(FieldGroupKeys.TrucksAndBuses) })
        {
            FieldDefinition km = group.Field("km")!;
            Assert.AreEqual(FieldType.Integer, km.Type, group.Key);
            Assert.AreEqual(0m, km.Min, group.Key);
            Assert.AreEqual(9_999_999m, km.Max, group.Key);
        }
    }

    [TestMethod]
    public void Pecas_Condicao_Obrigatoria_TipoECorOpcionais()
    {
        FieldGroup parts = Group(FieldGroupKeys.Parts);

        foreach (int category in new[] { 38, 39, 40, 41, 42 })
        {
            CollectionAssert.AreEqual(new[] { "conditionId" }, RequiredKeys(parts, category), $"categoria {category}");
            CollectionAssert.AreEqual(new[] { "conditionId", "partTypeId", "colorId" }, Keys(parts, category));
        }

        Assert.AreSame(FieldLists.PartCondition, parts.Field("conditionId")!.Options, "Peças tem a própria lista de condição");
        Assert.AreSame(FieldLists.PartColor, parts.Field("colorId")!.Options);
        Assert.IsFalse(parts.Field("partTypeId")!.Required);
        Assert.IsFalse(parts.Field("colorId")!.Required);
    }

    [TestMethod]
    public void Pecas_TipoDePeca_MudaPorCategoria()
    {
        FieldDefinition type = Group(FieldGroupKeys.Parts).Field("partTypeId")!;

        Assert.AreSame(FieldLists.AutoPartType, type.OptionsFor(38), "carros, vans e utilitários");
        Assert.AreSame(FieldLists.AutoPartType, type.OptionsFor(39), "caminhões");
        Assert.AreSame(FieldLists.MotorcyclePartType, type.OptionsFor(40), "motos");
        Assert.AreSame(FieldLists.BoatPartType, type.OptionsFor(41), "barcos e aeronaves");
        Assert.AreSame(FieldLists.AutoPartType, type.OptionsFor(42), "ônibus");
    }

    [TestMethod]
    public void TodosOsVeiculosEPecas_UsamOsLimitesPadraoDoFormulario()
    {
        foreach (string key in new[] { FieldGroupKeys.Cars, FieldGroupKeys.Motorcycles, FieldGroupKeys.TrucksAndBuses, FieldGroupKeys.BoatsAndAircraft, FieldGroupKeys.Parts })
        {
            FieldGroup group = Group(key);
            Assert.IsTrue(group.HasPrice, key);
            Assert.AreEqual("Preço", group.PriceLabel, key);
            Assert.AreEqual(20, group.MaxPhotos, key);
            Assert.AreEqual(120, group.TitleMaxLength, key);
            Assert.AreEqual(5000, group.DescriptionMaxLength, key);
            Assert.AreEqual("Descrição", group.DescriptionLabel, key);
        }
    }
}

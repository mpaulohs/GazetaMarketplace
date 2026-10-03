namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Chaves estáveis dos grupos de campos: é o que fica gravado em <c>Categories.FieldGroup</c>. Nunca renomeie uma chave em uso.
/// Os 18 grupos do Apêndice B (as tarefas 2.2 a 2.4 os trouxeram todos).
/// </summary>
public static class FieldGroupKeys
{
    public const string Services = "Services";

    public const string Jobs = "Jobs";

    /// <summary>Grupo padrão: vale para toda categoria sem grupo próprio na cadeia de ancestrais.</summary>
    public const string GeneralProducts = "GeneralProducts";

    public const string RealEstate = "RealEstate";

    public const string Cars = "Cars";

    public const string Motorcycles = "Motorcycles";

    public const string TrucksAndBuses = "TrucksAndBuses";

    public const string BoatsAndAircraft = "BoatsAndAircraft";

    /// <summary>Gravado em Autopeças (id 3); as filhas 38 a 42 herdam.</summary>
    public const string Parts = "Parts";

    public const string RoomRental = "RoomRental";

    public const string Seasonal = "Seasonal";

    public const string Phones = "Phones";

    public const string Smartwatches = "Smartwatches";

    public const string TelephonyProducts = "TelephonyProducts";

    public const string Appliances = "Appliances";

    public const string ElectronicsAndComputers = "ElectronicsAndComputers";

    public const string ClothingAndShoes = "ClothingAndShoes";

    public const string Machinery = "Machinery";
}

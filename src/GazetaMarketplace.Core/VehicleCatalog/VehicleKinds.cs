using System;

namespace GazetaMarketplace.Core.VehicleCatalog;

/// <summary>
/// O tipo de veículo do catálogo: <c>car</c> ou <c>moto</c>. No GazetaOnline carros e motos são tabelas separadas e cada uma numera os seus ids a
/// partir de 1; por isso toda chave do catálogo leva o tipo junto (a Honda de carros e a Honda de motos podem ter o mesmo id).
/// </summary>
public static class VehicleKinds
{
    public const string Car = "car";

    public const string Moto = "moto";

    public static bool IsValid(string kind) =>
        string.Equals(kind, Car, StringComparison.Ordinal) || string.Equals(kind, Moto, StringComparison.Ordinal);
}

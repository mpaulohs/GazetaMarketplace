using System;
using System.Collections.Generic;
using System.Linq;

namespace GazetaMarketplace.Core.Location;

/// <summary>Uma unidade federativa: sigla, código do IBGE (os dois primeiros dígitos do código de qualquer município dela) e nome.</summary>
public sealed record State(string Uf, int IbgeCode, string Name);

/// <summary>As 27 unidades federativas (UF) que o site aceita em CEP, cidade e filtro. Fica em código: não muda e dispensa tabela e endpoint.</summary>
public static class BrazilianStates
{
    public static IReadOnlyList<State> All { get; } =
    [
        new("AC", 12, "Acre"), new("AL", 27, "Alagoas"), new("AP", 16, "Amapá"), new("AM", 13, "Amazonas"), new("BA", 29, "Bahia"),
        new("CE", 23, "Ceará"), new("DF", 53, "Distrito Federal"), new("ES", 32, "Espírito Santo"), new("GO", 52, "Goiás"), new("MA", 21, "Maranhão"),
        new("MT", 51, "Mato Grosso"), new("MS", 50, "Mato Grosso do Sul"), new("MG", 31, "Minas Gerais"), new("PA", 15, "Pará"), new("PB", 25, "Paraíba"),
        new("PR", 41, "Paraná"), new("PE", 26, "Pernambuco"), new("PI", 22, "Piauí"), new("RJ", 33, "Rio de Janeiro"), new("RN", 24, "Rio Grande do Norte"),
        new("RS", 43, "Rio Grande do Sul"), new("RO", 11, "Rondônia"), new("RR", 14, "Roraima"), new("SC", 42, "Santa Catarina"), new("SP", 35, "São Paulo"),
        new("SE", 28, "Sergipe"), new("TO", 17, "Tocantins")
    ];

    /// <summary>A UF pela sigla (sem diferenciar maiúscula), ou nulo.</summary>
    public static State Find(string uf) =>
        string.IsNullOrWhiteSpace(uf) ? null : All.FirstOrDefault(s => s.Uf.Equals(uf.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsValid(string uf) => Find(uf) is not null;

    /// <summary>A UF a que pertence um código de município do IBGE (7 dígitos), pelos dois primeiros dígitos; nulo se o código não é de uma UF conhecida.</summary>
    public static State FromMunicipalityCode(int municipalityCode) =>
        municipalityCode is >= 1_000_000 and <= 9_999_999 ? All.FirstOrDefault(s => s.IbgeCode == municipalityCode / 100_000) : null;
}

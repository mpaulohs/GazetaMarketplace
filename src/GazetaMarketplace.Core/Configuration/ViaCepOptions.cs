using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuration;

/// <summary>Configuração do cliente do ViaCEP (ADR-007). Os padrões valem em produção; só os testes de ponta a ponta mudam o endereço.</summary>
public sealed class ViaCepOptions
{
    public const string SectionName = "ViaCep";

    /// <summary>Tempo limite de cada tentativa: 5 s (NFR-24).</summary>
    public const int DefaultTimeoutSeconds = 5;

    /// <summary>Endereço base; a rota fixa é <c>{cep}/json/</c>. Termina em barra.</summary>
    [Url]
    public string BaseUrl { get; set; } = "https://viacep.com.br/ws/";

    [Range(1, 30)]
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
}

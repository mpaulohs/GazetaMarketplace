using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Location;

/// <summary>Resposta do endpoint de CEP: <see cref="Source"/> diz se veio do cache do banco ou do serviço externo.</summary>
public sealed record CepResult(string Cep, string City, string Uf, string Source)
{
    public const string FromCache = "cache";

    public const string FromViaCep = "viacep";
}

/// <summary>Consulta de CEP para a equipe: cache de 30 dias no banco, depois o serviço externo, com o nome da cidade padronizado.</summary>
public interface ICepService
{
    /// <summary>Os dias em que uma entrada do cache vale (NFR-24).</summary>
    public const int CacheDays = 30;

    /// <exception cref="ValidationException">O CEP não tem exatamente 8 dígitos; nada é consultado (S23).</exception>
    /// <exception cref="NotFoundException">O CEP tem 8 dígitos mas não existe (S24); não abre o preenchimento manual e não entra no cache.</exception>
    /// <exception cref="ServiceUnavailableException">O serviço externo falhou; o cache vencido não é usado no lugar (ADR-007).</exception>
    Task<CepResult> GetAsync(string cep, CancellationToken cancellationToken);
}

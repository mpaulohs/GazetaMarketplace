using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Location;

/// <summary>O que o serviço externo de CEP devolve que o site usa: cidade, UF e código do IBGE. Rua e bairro não existem aqui de propósito.</summary>
public sealed record CepLookupResult(string City, string Uf, int? IbgeCode);

/// <summary>O serviço externo de CEP (ViaCEP, ADR-007), atrás de uma interface para trocar de provedor e para os testes usarem um falso.</summary>
public interface ICepLookup
{
    /// <summary>Uma tentativa de consulta, com tempo limite próprio.</summary>
    /// <returns>O resultado, ou nulo quando o provedor responde que o CEP não existe (não é falha do serviço).</returns>
    /// <exception cref="ServiceUnavailableException">Sem resposta no tempo, erro 5xx, sem rede ou resposta ilegível.</exception>
    Task<CepLookupResult> LookupAsync(string cep, CancellationToken cancellationToken);
}

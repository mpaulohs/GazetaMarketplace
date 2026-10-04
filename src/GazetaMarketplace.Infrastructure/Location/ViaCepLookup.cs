using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Location;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Infrastructure.Location;

/// <summary>
/// Cliente do ViaCEP (ADR-007): <c>GET {cep}/json/</c>, <b>uma</b> tentativa de até 5 s (o <c>Timeout</c> do <see cref="HttpClient"/>), sem Polly (ADR-012).
/// A nova tentativa é comandada pela tela. Só lê <c>localidade</c>, <c>uf</c>, <c>ibge</c> e <c>erro</c>: rua e bairro (<c>logradouro</c>, <c>bairro</c>) nunca são lidos
/// (NFR-19, S25). O formato do ViaCEP fica isolado aqui.
/// </summary>
public sealed class ViaCepLookup(HttpClient http, ICurrentUser currentUser, ILogger<ViaCepLookup> log) : ICepLookup
{
    public async Task<CepLookupResult> LookupAsync(string cep, CancellationToken cancellationToken)
    {
        if (!CepRules.IsValid(cep))
        {
            throw new ArgumentException("O CEP precisa ter 8 dígitos.", nameof(cep));
        }

        try
        {
            using HttpResponseMessage response = await http.GetAsync(new Uri(cep + "/json/", UriKind.Relative), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // 5xx, 429 e também 4xx: o ViaCEP não responde "CEP inexistente" com erro HTTP, e sim com 200 e {"erro": true}
                throw Unavailable(cep, $"HTTP {(int)response.StatusCode}", null);
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            return Parse(cep, body);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // o cliente desistiu
        }
        catch (OperationCanceledException error)
        {
            throw Unavailable(cep, "sem resposta no tempo limite", error);
        }
        catch (HttpRequestException error)
        {
            throw Unavailable(cep, "falha de rede", error);
        }
    }

    private CepLookupResult Parse(string cep, string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw Unavailable(cep, "resposta inesperada", null);
            }

            // {"erro": true} (às vezes "true", entre aspas): o CEP tem 8 dígitos mas não existe. Não é falha do serviço.
            if (root.TryGetProperty("erro", out JsonElement erro) && (erro.ValueKind == JsonValueKind.True || (erro.ValueKind == JsonValueKind.String && bool.TryParse(erro.GetString(), out bool flag) && flag)))
            {
                return null;
            }

            string city = Text(root, "localidade");
            string uf = Text(root, "uf");
            if (string.IsNullOrWhiteSpace(city) || !BrazilianStates.IsValid(uf))
            {
                throw Unavailable(cep, "resposta sem cidade ou UF válidas", null);
            }

            int? ibge = int.TryParse(Text(root, "ibge"), NumberStyles.None, CultureInfo.InvariantCulture, out int code) ? code : null;
            return new CepLookupResult(city.Trim(), BrazilianStates.Find(uf).Uf, ibge);
        }
        catch (JsonException error)
        {
            throw Unavailable(cep, "resposta ilegível", error);
        }
    }

    private static string Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private ServiceUnavailableException Unavailable(string cep, string reason, Exception error)
    {
        // Warning com o CEP e o traceId (ADR-007); o CEP não identifica um endereço completo
        log.LogWarning(error, "ViaCEP indisponível para o CEP {Cep} ({Reason}). TraceId {TraceId}", cep, reason, currentUser.CorrelationId);
        return new ServiceUnavailableException(CepRules.UnavailableMessage);
    }
}

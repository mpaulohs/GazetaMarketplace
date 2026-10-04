using System;

namespace GazetaMarketplace.Core.Location;

/// <summary>
/// Um CEP encontrado, guardado no banco por 30 dias (NFR-24, ADR-007) para sobreviver à reciclagem do IIS. Só cidade, UF e código do IBGE:
/// <b>rua e bairro nunca são lidos nem guardados</b> (NFR-19, S25: o CEP pode indicar o endereço do dono do bem).
/// </summary>
public class CepCacheEntry
{
    /// <summary>Os 8 dígitos.</summary>
    public string Cep { get; set; }

    public string City { get; set; }

    public string Uf { get; set; }

    /// <summary>Código do município no IBGE, quando o ViaCEP o informa.</summary>
    public int? IbgeCode { get; set; }

    public DateTime FetchedAt { get; set; }
}

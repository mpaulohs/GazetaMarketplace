namespace GazetaMarketplace.Core.Location;

/// <summary>
/// Município da lista oficial do IBGE (tabela <c>Cities</c>), para a padronização do nome e para o preenchimento manual. Tabela de referência:
/// só é carregada por script e nunca editada pelo site, então não tem auditoria nem <c>RowVersion</c>.
/// </summary>
public class City
{
    /// <summary>Código do município no IBGE (7 dígitos); os dois primeiros são os da UF.</summary>
    public int IbgeCode { get; set; }

    /// <summary>Nome oficial, com acentos.</summary>
    public string Name { get; set; }

    public string Uf { get; set; }

    /// <summary>Nome normalizado (<c>Normalizer</c>) para comparar sem acento nem maiúscula; gerado pelo C# na carga.</summary>
    public string NameSearch { get; set; }
}

/// <summary>Uma cidade como a tela precisa dela: o código e o nome para a lista do preenchimento manual.</summary>
public sealed record CityItem(int IbgeCode, string Name);

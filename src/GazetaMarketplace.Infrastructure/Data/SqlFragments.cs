namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>
/// Fragmentos de SQL compartilhados pelas leituras Dapper. O Dapper não herda filtros do EF, então
/// o "só publicados" (US-003-S06) vive num único lugar. Convenção: a tabela Ads usa o alias <c>a</c>.
/// </summary>
public static class SqlFragments
{
    public const string ParametroStatusPublicado = "StatusPublicado";

    /// <summary>Valor de "Publicado" em Ads.Status (ARCHITECTURE.md §6.2). A tarefa 3.1 cria o enum SituacaoAnuncio e um teste de igualdade.</summary>
    public const byte StatusPublicado = 3;

    public const string SomentePublicados = "a.Status = @" + ParametroStatusPublicado;
}

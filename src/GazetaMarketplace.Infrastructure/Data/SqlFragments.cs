using GazetaMarketplace.Core.Ads;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>
/// Fragmentos de SQL compartilhados pelas leituras Dapper. O Dapper não herda filtros do EF, então
/// o "só publicados" (US-003-S06) vive num único lugar. Convenção: a tabela Ads usa o alias <c>a</c>.
/// </summary>
public static class SqlFragments
{
    public const string PublishedStatusParameter = "PublishedStatus";

    /// <summary>Valor de "Publicado" em Ads.Status (ARCHITECTURE.md §6.2), sempre o de <see cref="AdStatus.Published"/>.</summary>
    public const byte PublishedStatus = AdStatus.Published;

    public const string OnlyPublished = "a.Status = @" + PublishedStatusParameter;

    /// <summary>"Esta situação", para a lista do painel (a situação vem de um filtro escolhido, não é só "publicado").</summary>
    public const string StatusParameter = "Status";

    public const string StatusIs = "a.Status = @" + StatusParameter;

    /// <summary>"Sem os arquivados": a lista padrão do painel (US-011-S05, US-012).</summary>
    public const string ArchivedStatusParameter = "ArchivedStatus";

    public const string NotArchived = "a.Status <> @" + ArchivedStatusParameter;
}

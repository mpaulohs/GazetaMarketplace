using System.Collections.Generic;
using System.Linq;

namespace GazetaMarketplace.Core.Ads;

/// <summary>Uma passagem permitida entre duas situações (SPEC, Apêndice A). <see cref="Action"/> é o nome gravado na auditoria.</summary>
/// <param name="From">Situação de partida.</param>
/// <param name="To">Situação de chegada.</param>
/// <param name="AuthorAllowed">Se o autor (Redator) pode fazer a passagem; as demais são só do Administrador.</param>
/// <param name="Action">Ação da auditoria.</param>
public sealed record AdTransition(byte From, byte To, bool AuthorAllowed, string Action);

/// <summary>A tabela de passagens do Apêndice A: é a única fonte; o serviço e a entidade só consultam esta lista.</summary>
public static class AdStatusRules
{
    public static IReadOnlyList<AdTransition> All { get; } =
    [
        new(AdStatus.Draft, AdStatus.InReview, true, "ad.submit"),
        new(AdStatus.Rejected, AdStatus.InReview, true, "ad.submit"),
        new(AdStatus.InReview, AdStatus.Published, false, "ad.publish"),
        new(AdStatus.InReview, AdStatus.Rejected, false, "ad.reject"),
        new(AdStatus.Published, AdStatus.Draft, false, "ad.unpublish"),
        new(AdStatus.Draft, AdStatus.Archived, false, "ad.archive"),
        new(AdStatus.InReview, AdStatus.Archived, false, "ad.archive"),
        new(AdStatus.Rejected, AdStatus.Archived, false, "ad.archive"),
        new(AdStatus.Published, AdStatus.Archived, false, "ad.archive")
    ];

    /// <summary>A passagem de uma situação para outra, ou nulo se o Apêndice A não a prevê (Arquivado é definitivo).</summary>
    public static AdTransition Find(byte from, byte to) => All.FirstOrDefault(t => t.From == from && t.To == to);
}

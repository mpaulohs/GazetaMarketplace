namespace GazetaMarketplace.Core.Ads;

/// <summary>Quem está agindo: o id do usuário (nulo = sistema ou anônimo) e se é Administrador. Quem não é Administrador é tratado como Redator.</summary>
public sealed record AdActor(int? UserId, bool IsAdministrator);

/// <summary>
/// Quem lê, edita e muda a situação de cada anúncio (SPEC, US-008 e Apêndice A). Regra pura: o <c>IAdService</c> a aplica no servidor, então
/// uma tela esquecida não abre brecha (NFR-13).
/// </summary>
/// <remarks>
/// O Redator <b>lê</b> os próprios anúncios em qualquer situação (para ver "Em revisão" em somente leitura, US-008-S12, e listar os publicados e
/// arquivados em "Meus anúncios") e <b>edita</b> só em Rascunho ou Rejeitado. O Administrador lê todos e edita os que não estão Arquivados.
/// </remarks>
public static class AdAccess
{
    public static bool CanView(AdActor actor, Ad ad) => CanView(actor, ad.AuthorId);

    /// <summary>A mesma regra de leitura quando só o autor do anúncio é conhecido (a entrega de fotos não carrega o anúncio inteiro).</summary>
    public static bool CanView(AdActor actor, int authorId) => actor.IsAdministrator || (actor.UserId is { } id && id == authorId);

    public static bool CanEdit(AdActor actor, Ad ad)
    {
        if (actor.IsAdministrator)
        {
            return ad.Status != AdStatus.Archived;
        }

        return IsAuthor(actor, ad) && ad.Status is AdStatus.Draft or AdStatus.Rejected;
    }

    /// <summary>Se o ator pode fazer esta passagem: o Administrador qualquer uma do Apêndice A; o autor só as marcadas para ele.</summary>
    public static bool CanTransition(AdActor actor, Ad ad, AdTransition transition) =>
        actor.IsAdministrator || (transition.AuthorAllowed && IsAuthor(actor, ad));

    private static bool IsAuthor(AdActor actor, Ad ad) => actor.UserId is { } id && id == ad.AuthorId;
}

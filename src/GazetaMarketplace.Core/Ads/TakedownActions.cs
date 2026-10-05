namespace GazetaMarketplace.Core.Ads;

/// <summary>Quais ações de retirada a tela mostra para quem a abre (US-011-S06 e S07): só o Administrador, e só as que o Apêndice A permite na situação do anúncio.</summary>
public sealed record TakedownActions(bool CanUnpublish, bool CanArchive)
{
    public static TakedownActions None { get; } = new(false, false);

    public bool Any => CanUnpublish || CanArchive;

    public static TakedownActions For(AdActor actor, Ad ad) => new(Allows(actor, ad, AdStatus.Draft, AdStatus.Published), Allows(actor, ad, AdStatus.Archived, null));

    // Despublicar é a passagem para Rascunho que parte de Publicado (Rascunho também é destino de nada mais no Apêndice A, mas a conferência da origem deixa isso explícito)
    private static bool Allows(AdActor actor, Ad ad, byte target, byte? requiredFrom) =>
        (requiredFrom is null || ad.Status == requiredFrom)
        && AdStatusRules.Find(ad.Status, target) is { } transition
        && AdAccess.CanTransition(actor, ad, transition);
}

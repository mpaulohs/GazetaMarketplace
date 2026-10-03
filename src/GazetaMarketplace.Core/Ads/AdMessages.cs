namespace GazetaMarketplace.Core.Ads;

/// <summary>Textos das recusas do anúncio, como estão na SPEC (US-008-S10, S12) ou, onde a SPEC não diz, os mais simples.</summary>
public static class AdMessages
{
    public const string NoPermission = "Você não tem permissão para acessar este anúncio";

    public const string InReviewReadOnly = "Este anúncio está em revisão e não pode ser editado";

    public const string NotEditable = "Este anúncio não pode ser editado";

    public const string NotFound = "Anúncio não encontrado";

    public const string RejectionReasonRequired = "Informe o motivo da rejeição";

    public static string RejectionReasonTooLong(int max) => $"O motivo pode ter no máximo {max} caracteres";

    public static string InvalidTransition(byte from, byte to) =>
        $"Não é possível mudar o anúncio de \"{AdStatus.Label(from)}\" para \"{AdStatus.Label(to)}\"";

    public const string TitleRequired = "Informe um título";
}

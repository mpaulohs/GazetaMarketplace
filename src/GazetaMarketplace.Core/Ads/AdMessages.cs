namespace GazetaMarketplace.Core.Ads;

/// <summary>Textos das recusas do anúncio, como estão na SPEC (US-008-S10, S12) ou, onde a SPEC não diz, os mais simples.</summary>
public static class AdMessages
{
    public const string NoPermission = "Você não tem permissão para acessar este anúncio";

    public const string InReviewReadOnly = "Este anúncio está em revisão e não pode ser editado";

    public const string NotEditable = "Este anúncio não pode ser editado";

    public const string NotFound = "Anúncio não encontrado";

    public const string Published = "Anúncio publicado";

    public const string Rejected = "Anúncio rejeitado";

    public const string PhoneNotConfigured = "Configure o telefone/WhatsApp do site antes de publicar";

    /// <summary>
    /// A frase para quem tentou decidir um anúncio que já não está Em revisão. Entre Administradores diferentes vale a frase da SPEC (US-010-S07, "por outro administrador");
    /// o mesmo Administrador num clique duplo ou em duas abas ouve só "já foi publicado".
    /// </summary>
    public static string AlreadyDecided(byte currentStatus, bool sameAdministrator) => currentStatus switch
    {
        AdStatus.Published => sameAdministrator ? "Este anúncio já foi publicado" : "Este anúncio já foi publicado por outro administrador",
        AdStatus.Rejected => sameAdministrator ? "Este anúncio já foi rejeitado" : "Este anúncio já foi rejeitado por outro administrador",
        AdStatus.Archived => "Este anúncio foi arquivado",
        _ => "Este anúncio não está mais em revisão"
    };

    public const string Unpublished = "Anúncio despublicado";

    public const string Archived = "Anúncio arquivado";

    public const string AlreadyArchived = "Este anúncio já foi arquivado";

    public static string NoLongerPublished(byte currentStatus) => $"Este anúncio não está mais publicado. Situação: {AdStatus.Label(currentStatus)}";

    public const string RejectionReasonRequired = "Informe o motivo da rejeição";

    public static string RejectionReasonTooLong(int max) => $"O motivo pode ter no máximo {max} caracteres";

    public static string InvalidTransition(byte from, byte to) =>
        $"Não é possível mudar o anúncio de \"{AdStatus.Label(from)}\" para \"{AdStatus.Label(to)}\"";

    public const string TitleRequired = "Informe um título";

    public const string DraftSaved = "Rascunho salvo";

    public const string CategoryInvalid = "Escolha uma categoria da lista";

    public const string CategoryGone = "A categoria escolhida não existe mais. Escolha outra";

    public const string PriceOutOfRange = "Informe um preço entre R$ 0,01 e R$ 99.999.999,99";

    public const string PriceInvalid = "Informe o preço em reais, por exemplo 62.000,00";

    public const string CityRequiredWithUf = "Informe a cidade";

    public const string UfInvalid = "Escolha um estado da lista";

    public const string CityNotInUf = "Escolha uma cidade da lista";

    public const string LocationOnlyWithCep = "Informe o CEP para definir a cidade";

    public static string TitleTooLong(int max) => $"O título pode ter no máximo {max} caracteres";

    public static string DescriptionTooLong(string label, int max) => $"Use no máximo {max} caracteres em \"{label}\"";

    public static string TooManyPhotosForCategory(int max) => $"Esta categoria aceita no máximo {max} fotos; remova fotos antes de trocar";

    public const string CategoryRequired = "Escolha uma categoria";

    /// <summary>"Informe a descrição" (SPEC, S22); em Serviços e Vagas, onde o campo se chama "Informações adicionais", o nome do campo.</summary>
    public static string DescriptionRequired(string label) => label == "Descrição" ? "Informe a descrição" : $"Informe as {label.ToLowerInvariant()}";

    public static string PriceRequired(string label) => label == "Salário" ? "Informe o salário" : "Informe o preço";

    public const string CepRequired = "Informe o CEP";

    public static string PhotosRequired(int min) => min == 1 ? "Adicione ao menos 1 foto" : $"Adicione ao menos {min} fotos";

    public const string AlreadySubmitted = "Este anúncio já foi enviado para revisão";

    public const string Submitted = "Anúncio enviado para revisão";
}


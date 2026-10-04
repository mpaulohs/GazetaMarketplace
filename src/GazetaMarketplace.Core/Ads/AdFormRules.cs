using System;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Formatting;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// As regras do formulário que dependem do grupo da categoria (Apêndice B): tamanho do título e da descrição, preço, fotos. Lê tudo do
/// <see cref="FieldGroup"/> — a única fonte desses números — e é a mesma regra no servidor e no contador da tela.
/// </summary>
public static class AdFormRules
{
    /// <summary>
    /// O navegador envia quebra de linha como CRLF (2 caracteres) enquanto o contador da tela conta 1 por quebra. O servidor guarda e conta com LF
    /// só, para o número que a pessoa viu ser o mesmo que o servidor confere.
    /// </summary>
    public static string NormalizeLineBreaks(string text) =>
        text?.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    /// <summary>Mensagem de recusa do título, ou nulo se está bom. Título vazio é recusado (US-008-S08).</summary>
    public static string TitleError(FieldGroup group, string title)
    {
        ArgumentNullException.ThrowIfNull(group);
        string trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return AdMessages.TitleRequired;
        }

        return trimmed.Length > group.TitleMaxLength ? AdMessages.TitleTooLong(group.TitleMaxLength) : null;
    }

    /// <summary>Mensagem de recusa da descrição, ou nulo se está boa. A descrição pode ficar vazia no rascunho.</summary>
    public static string DescriptionError(FieldGroup group, string description)
    {
        ArgumentNullException.ThrowIfNull(group);
        return description is not null && description.Length > group.DescriptionMaxLength
            ? AdMessages.DescriptionTooLong(group.DescriptionLabel, group.DescriptionMaxLength)
            : null;
    }

    /// <summary>
    /// Lê o preço digitado. Grupo sem preço (Serviços) ignora o campo e devolve vazio. Devolve a mensagem de recusa ou nulo; em <paramref name="cents"/>
    /// vai o valor em centavos, ou nulo quando o campo está vazio.
    /// </summary>
    public static string PriceError(FieldGroup group, string text, out long? cents)
    {
        ArgumentNullException.ThrowIfNull(group);
        cents = null;
        if (!group.HasPrice)
        {
            return null;
        }

        switch (PriceText.TryParse(text, out long parsed))
        {
            case PriceParse.Empty:
                return null;
            case PriceParse.Invalid:
                return AdMessages.PriceInvalid;
            default:
                if (parsed is <= 0 or > FieldLimits.MaxMoneyCents)
                {
                    return AdMessages.PriceOutOfRange;
                }

                cents = parsed;
                return null;
        }
    }

    /// <summary>Se o rascunho com <paramref name="photoCount"/> fotos cabe no grupo; senão, a mensagem da troca de categoria (D9).</summary>
    public static string PhotoLimitError(FieldGroup group, int photoCount)
    {
        ArgumentNullException.ThrowIfNull(group);
        return photoCount > group.MaxPhotos ? AdMessages.TooManyPhotosForCategory(group.MaxPhotos) : null;
    }
}

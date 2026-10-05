using System;
using System.Text;

namespace GazetaMarketplace.Core.Contact;

/// <summary>
/// O link "Chamar no WhatsApp" (US-004): <c>https://wa.me/55{número}?text={mensagem}</c>. A mensagem é fixa (cumprimento, título do anúncio e endereço da página) e vai
/// <b>codificada como URL</b>, então acentos, aspas, "&amp;", "#", "%" e emoji chegam ao WhatsApp exatamente como estão no título. O título é cortado em
/// <see cref="TitleMaxLength"/> caracteres (sem reticências) para o link ficar com folga abaixo do limite de endereço do WhatsApp.
/// </summary>
public static class WhatsAppLink
{
    /// <summary>Tamanho máximo do título dentro da mensagem.</summary>
    public const int TitleMaxLength = 120;

    /// <summary>O texto da conversa, antes de codificar: <c>Olá! Tenho interesse no anúncio “{título}”: {endereço}</c>.</summary>
    public static string Message(string title, string pageUrl)
    {
        string cut = CutTitle(title);
        return cut.Length == 0
            ? $"Olá! Tenho interesse neste anúncio: {pageUrl}"
            : $"Olá! Tenho interesse no anúncio “{cut}”: {pageUrl}";
    }

    /// <summary>O endereço completo do link, com o número (só dígitos, sem o 55) e a mensagem codificada.</summary>
    public static string Build(string phoneDigits, string title, string pageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneDigits);
        ArgumentException.ThrowIfNullOrWhiteSpace(pageUrl);
        return $"https://wa.me/55{phoneDigits}?text={Uri.EscapeDataString(Message(title, pageUrl))}";
    }

    // Corta sem partir um par substituto (emoji): se o corte cair no meio de um, o par inteiro fica de fora
    private static string CutTitle(string title)
    {
        string text = (title ?? string.Empty).Trim();
        if (text.Length <= TitleMaxLength)
        {
            return text;
        }

        int length = char.IsHighSurrogate(text[TitleMaxLength - 1]) ? TitleMaxLength - 1 : TitleMaxLength;
        return text[..length].TrimEnd();
    }
}

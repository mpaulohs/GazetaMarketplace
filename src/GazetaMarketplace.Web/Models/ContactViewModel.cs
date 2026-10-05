using GazetaMarketplace.Core.Contact;
using GazetaMarketplace.Core.Settings;

namespace GazetaMarketplace.Web.Models;

/// <summary>
/// O bloco "Fale com a Gazeta" (US-004): o telefone do site escrito e os dois links. Os links são comuns (sem JavaScript, sem passar pelo servidor): o site não registra
/// quem clicou. Nulo quando o telefone ainda não foi configurado, e então o bloco não aparece. <see cref="WhatsAppHref"/> é nulo para telefone fixo (o WhatsApp só abre conversa de celular): só o "Ligar" aparece.
/// </summary>
public sealed record ContactViewModel(string Display, string TelHref, string WhatsAppHref)
{
    public static ContactViewModel Create(string phoneDigits, string title, string pageUrl) =>
        string.IsNullOrWhiteSpace(phoneDigits)
            ? null
            : new ContactViewModel(
                PhoneNumber.Format(phoneDigits),
                PhoneLink.Tel(phoneDigits),
                WhatsAppLink.IsAvailableFor(phoneDigits) ? WhatsAppLink.Build(phoneDigits, title, pageUrl) : null);
}

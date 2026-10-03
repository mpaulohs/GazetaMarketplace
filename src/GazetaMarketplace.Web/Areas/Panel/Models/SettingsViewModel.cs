namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>Configurações do site (US-015). Por enquanto só o telefone/WhatsApp; novas chaves entram como novas propriedades.</summary>
public sealed class SettingsViewModel
{
    /// <summary>Número como a pessoa digitou ou como está guardado, já formatado para exibição.</summary>
    public string Phone { get; set; }

    /// <summary>Verdadeiro quando ainda não há número salvo: a tela avisa que o botão de contato do site fica fora do ar.</summary>
    public bool IsPhoneConfigured { get; init; }

    public string Message { get; init; }
}

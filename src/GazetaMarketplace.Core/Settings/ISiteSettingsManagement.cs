using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Settings;

/// <summary>Textos da tela de configurações (US-015), como estão na SPEC.</summary>
public static class SiteSettingsMessages
{
    public const string Saved = "Configurações salvas";

    public const string PhoneRequired = "O telefone é obrigatório";

    public const string PhoneInvalid = "Informe um número com DDD, por exemplo (11) 91234-5678";

    public const string Conflict = "As configurações foram alteradas por outra pessoa. Recarregue a página e tente de novo.";
}

/// <summary>Resultado de salvar uma configuração: deu certo ou traz a mensagem para mostrar ao Administrador.</summary>
public sealed record SettingsSaveResult(bool Succeeded, string Error)
{
    public static SettingsSaveResult Success { get; } = new(true, null);

    public static SettingsSaveResult Failure(string error) => new(false, error);
}

/// <summary>Escrita das configurações do site (só o Administrador). Cada gravação vai para a auditoria com o valor anterior e o novo (RC-16).</summary>
public interface ISiteSettingsManagement
{
    /// <summary>
    /// Valida e grava o telefone/WhatsApp (US-015). Número vazio ou inválido não muda nada. Igual ao que já está gravado: sucesso sem gravar
    /// nem auditar. Esvazia o cache do <see cref="ISiteSettings"/> ao gravar.
    /// </summary>
    Task<SettingsSaveResult> SavePhoneAsync(string input, CancellationToken cancellationToken);
}

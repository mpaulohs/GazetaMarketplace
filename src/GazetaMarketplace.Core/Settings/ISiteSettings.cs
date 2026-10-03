using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Settings;

/// <summary>
/// Leitura das configurações do site, em cache de memória (10 minutos) esvaziado ao salvar. O cache é <b>por processo</b>: com mais de um
/// processo do site, a troca de um valor aparece nos outros em até 10 minutos (aceito na v1, um processo só).
/// </summary>
public interface ISiteSettings
{
    /// <summary>O valor da chave, ou nulo se nunca foi configurada.</summary>
    Task<string> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>O telefone/WhatsApp do site: só dígitos, sem o +55; nulo se ainda não foi configurado.</summary>
    Task<string> GetPhoneAsync(CancellationToken cancellationToken);

    /// <summary>Há telefone configurado? Sem ele nenhum anúncio pode ser publicado (US-010-S08).</summary>
    Task<bool> IsPhoneConfiguredAsync(CancellationToken cancellationToken);

    /// <summary>Descarta o cache; a próxima leitura vai ao banco.</summary>
    void Invalidate();
}

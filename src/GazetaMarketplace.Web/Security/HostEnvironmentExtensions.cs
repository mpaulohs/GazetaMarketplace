using Microsoft.Extensions.Hosting;

namespace GazetaMarketplace.Web.Security;

public static class HostEnvironmentExtensions
{
    /// <summary>
    /// Ambiente com as proteções de produção ligadas (V-05, SC-09): HSTS, remetente SendGrid, porta HTTPS 443 e validação de toda a configuração na partida.
    /// É tudo que <b>não</b> é <c>Development</c> nem <c>Testing</c>: <c>Production</c>, <c>Staging</c> e qualquer nome que a hospedagem usar. Antes valia só o nome exato
    /// "Production", e um site de homologação chamado "Staging" subiria sem validar o endereço público e montaria o link de redefinição pelo cabeçalho Host.
    /// </summary>
    public static bool IsHardened(this IHostEnvironment environment) =>
        !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
}

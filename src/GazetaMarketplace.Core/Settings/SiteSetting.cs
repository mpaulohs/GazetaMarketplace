using GazetaMarketplace.Core.Entities;

namespace GazetaMarketplace.Core.Settings;

/// <summary>
/// Uma configuração do site em formato chave-valor (tela "Configurações"). A chave é estável e única (<see cref="SiteSettingKeys"/>); o valor é
/// sempre texto. Tem auditoria e <c>RowVersion</c>: dois Administradores salvando ao mesmo tempo não se sobrescrevem em silêncio.
/// </summary>
public class SiteSetting : BaseEntity
{
    public string Key { get; set; }

    public string Value { get; set; }
}

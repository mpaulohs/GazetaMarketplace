using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Settings;

/// <inheritdoc cref="ISiteSettingsManagement"/>
public sealed class SiteSettingsManagement(AppDbContext db, IAuditLog audit, ISiteSettings settings) : ISiteSettingsManagement
{
    private const string TargetType = "SiteSetting";

    public async Task<SettingsSaveResult> SavePhoneAsync(string input, CancellationToken cancellationToken)
    {
        switch (PhoneNumber.TryNormalize(input, out string digits))
        {
            case PhoneParseResult.Empty:
                return SettingsSaveResult.Failure(SiteSettingsMessages.PhoneRequired);
            case PhoneParseResult.Invalid:
                return SettingsSaveResult.Failure(SiteSettingsMessages.PhoneInvalid);
        }

        SiteSetting setting = await db.SiteSettings.SingleOrDefaultAsync(s => s.Key == SiteSettingKeys.Phone, cancellationToken);
        string previous = setting?.Value;
        if (previous == digits)
        {
            return SettingsSaveResult.Success; // nada mudou: não grava nem audita
        }

        if (setting is null)
        {
            db.SiteSettings.Add(new SiteSetting { Key = SiteSettingKeys.Phone, Value = digits });
        }
        else
        {
            setting.Value = digits;
        }

        try
        {
            // A auditoria grava no mesmo SaveChanges da configuração: ou saem as duas, ou nenhuma (RC-16)
            await audit.RecordAsync(new AuditRecord("site_settings.change_phone", TargetType, SiteSettingKeys.Phone, AuditResult.Success, previous, digits), cancellationToken);
        }
        catch (ConflictException)
        {
            return SettingsSaveResult.Failure(SiteSettingsMessages.Conflict);
        }
        catch (DbUpdateException)
        {
            // Duas pessoas criando a primeira configuração ao mesmo tempo: o índice único recusa a segunda
            return SettingsSaveResult.Failure(SiteSettingsMessages.Conflict);
        }

        settings.Invalidate();
        return SettingsSaveResult.Success;
    }
}

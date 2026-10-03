using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Settings;

/// <summary>O cache de 10 minutos das configurações (relógio do site) e a invalidação ao salvar pela tela.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SiteSettingsStoreTests
#pragma warning restore CA1515
{
    private static async Task ChangeBehindTheCacheAsync(WebFactory factory, string digits)
    {
        // Direto no banco, sem passar pelo serviço: o cache não fica sabendo
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        SiteSetting setting = await context.SiteSettings.SingleOrDefaultAsync(s => s.Key == SiteSettingKeys.Phone);
        if (setting is null)
        {
            context.SiteSettings.Add(new SiteSetting { Key = SiteSettingKeys.Phone, Value = digits });
        }
        else
        {
            setting.Value = digits;
        }

        await context.SaveChangesAsync();
    }

    private static Task<string> ReadAsync(WebFactory factory) => factory.Services.GetRequiredService<ISiteSettings>().GetPhoneAsync(CancellationToken.None);

    [TestMethod]
    public async Task SemNenhumaLinha_NaoHaTelefone()
    {
        using WebFactory factory = new(withDatabase: true);

        Assert.IsNull(await ReadAsync(factory));
        Assert.IsFalse(await factory.Services.GetRequiredService<ISiteSettings>().IsPhoneConfiguredAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task OValorFicaEmCacheAte10Minutos_ENaoUmSegundoAMais()
    {
        using WebFactory factory = new(withDatabase: true);
        await ChangeBehindTheCacheAsync(factory, "11912345678");
        Assert.AreEqual("11912345678", await ReadAsync(factory));

        await ChangeBehindTheCacheAsync(factory, "21987654321");

        factory.Clock.Now += TimeSpan.FromMinutes(9) + TimeSpan.FromSeconds(59);
        Assert.AreEqual("11912345678", await ReadAsync(factory), "aos 9min59 ainda vale o cache");
        factory.Clock.Now += TimeSpan.FromSeconds(1);
        Assert.AreEqual("21987654321", await ReadAsync(factory), "aos 10 minutos relê o banco");
    }

    [TestMethod]
    public async Task Invalidate_ForcaReleituraNaHora()
    {
        using WebFactory factory = new(withDatabase: true);
        await ChangeBehindTheCacheAsync(factory, "11912345678");
        _ = await ReadAsync(factory);
        await ChangeBehindTheCacheAsync(factory, "21987654321");

        factory.Services.GetRequiredService<ISiteSettings>().Invalidate();

        Assert.AreEqual("21987654321", await ReadAsync(factory));
    }

    [TestMethod]
    public async Task ChaveInexistente_DevolveNulo()
    {
        using WebFactory factory = new(withDatabase: true);
        await ChangeBehindTheCacheAsync(factory, "11912345678");

        Assert.IsNull(await factory.Services.GetRequiredService<ISiteSettings>().GetAsync("site.outra", CancellationToken.None));
    }
}

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Cep;

/// <summary>
/// R-03: duas pessoas gravam o mesmo CEP novo ao mesmo tempo; o INSERT de uma bate na chave primária e o <see cref="GazetaMarketplace.Infrastructure.Location.CepService"/> segue com a resposta que já tem.
/// Essa recusa não pode soltar o anúncio que a mesma requisição está editando: antes, o <c>ChangeTracker.Clear()</c> o desanexava e a edição respondia sucesso sem gravar o título (e com a auditoria gravada).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CepRaceOnEditTests
#pragma warning restore CA1515
{
    // Grava o mesmo CEP por outro caminho entre o SELECT e o INSERT do cache (uma vez só)
    private sealed class OtherPersonStoresTheSameCep : SaveChangesInterceptor
    {
        private int _fired;

        public bool Fired => _fired == 1;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            bool addingCep = false;
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<CepCacheEntry> entry in eventData.Context.ChangeTracker.Entries<CepCacheEntry>())
            {
                addingCep |= entry.State == EntityState.Added;
            }

            if (addingCep && Interlocked.Exchange(ref _fired, 1) == 0)
            {
                await eventData.Context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO CepCache (Cep, City, Uf, IbgeCode, FetchedAt) VALUES ('13015100', 'Campinas', 'SP', 3509502, '2026-01-01 00:00:00')", cancellationToken);
            }

            return result;
        }
    }

    [TestMethod]
    public async Task EdicaoDeRascunho_ComOMesmoCepGravadoPorOutraPessoaNoMeio_GravaOTitulo_ELinhaDeAuditoriaSoUma()
    {
        OtherPersonStoresTheSameCep race = new();
        using DraftSite site = await DraftSite.StartAsync(services: s => s.AddDbContext<AppDbContext>(o => o.AddInterceptors(race)));
        int id = await site.AddAdAsync(PanelFixture.WriterEmail, AdStatus.Draft, "Rascunho original");

        HttpResponseMessage saved = await DraftSite.PostAsync(site.Writer, $"/painel/anuncios/{id}/editar", $"/painel/anuncios/{id}/editar", ("Title", "Título novo"), ("Cep", "13015-100"));

        Assert.IsTrue(race.Fired, "a corrida aconteceu de verdade");
        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        Ad ad = await site.LoadAsync(id);
        Assert.AreEqual("Título novo", ad.Title, "a edição não se perde");
        Assert.AreEqual("13015100", ad.Cep);
        Assert.AreEqual("Campinas", ad.City);
        Assert.HasCount(1, await site.AuditAsync(), "a auditoria diz o que de fato foi gravado");
        Assert.HasCount(1, await site.Harness.WithDbAsync(db => db.CepCache.AsNoTracking().ToListAsync()), "e o cache tem uma linha só");
    }
}

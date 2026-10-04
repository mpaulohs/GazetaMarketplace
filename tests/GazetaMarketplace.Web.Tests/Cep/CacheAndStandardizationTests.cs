using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Location;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Cep;

/// <summary>O cache de 30 dias em tabela e a padronização do nome da cidade, no <see cref="CepService"/> real sobre SQLite.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CacheAndStandardizationTests
#pragma warning restore CA1515
{
    private const string Cep = "13015100";

    private sealed class Rig : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ServiceProvider _services;

        public Rig()
        {
            _connection.Open();
            Clock = new FakeClock();
            Lookup = new FakeCepLookup();
            ServiceCollection services = new();
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
            services.AddSingleton<ICepLookup>(Lookup);
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<ICityDirectory, CityDirectory>();
            services.AddScoped<ICepService, CepService>();
            _services = services.BuildServiceProvider();
            using IServiceScope scope = _services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        }

        public FakeClock Clock { get; }

        public FakeCepLookup Lookup { get; }

        public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
        {
            using IServiceScope scope = _services.CreateScope();
            return await work(scope.ServiceProvider);
        }

        public Task<CepResult> GetAsync(string cep = Cep) => InScopeAsync(p => p.GetRequiredService<ICepService>().GetAsync(cep, CancellationToken.None));

        public Task<System.Collections.Generic.List<CepCacheEntry>> CacheAsync() =>
            InScopeAsync(p => p.GetRequiredService<AppDbContext>().CepCache.AsNoTracking().OrderBy(e => e.Cep).ToListAsync());

        public Task AddCityAsync(int code, string name, string uf) => InScopeAsync(async p =>
        {
            AppDbContext db = p.GetRequiredService<AppDbContext>();
            db.Cities.Add(new City { IbgeCode = code, Name = name, Uf = uf, NameSearch = GazetaMarketplace.Core.Search.Normalizer.Normalize(name) });
            await db.SaveChangesAsync();
            return 0;
        });

        public void Dispose()
        {
            _services.Dispose();
            _connection.Dispose();
        }
    }

    [TestMethod]
    public async Task CepEncontrado_ValeTrintaDias()
    {
        using Rig rig = new();

        CepResult first = await rig.GetAsync();
        rig.Clock.Now = rig.Clock.Now.AddDays(29).AddHours(23);
        CepResult almost = await rig.GetAsync();
        rig.Clock.Now = rig.Clock.Now.AddHours(1); // completam 30 dias exatos
        CepResult expired = await rig.GetAsync();

        Assert.AreEqual("viacep", first.Source);
        Assert.AreEqual("cache", almost.Source, "com 29 dias e 23 horas ainda vale");
        Assert.AreEqual("viacep", expired.Source, "aos 30 dias a entrada vence e o serviço é consultado de novo");
        Assert.AreEqual(2, rig.Lookup.Calls);
    }

    [TestMethod]
    public async Task EntradaVencida_EhAtualizadaNoLugar_SemDuplicar()
    {
        using Rig rig = new();
        await rig.GetAsync();
        rig.Clock.Now = rig.Clock.Now.AddDays(31);
        rig.Lookup.Respond = _ => new CepLookupResult("Valinhos", "SP", 3556206);

        CepResult refreshed = await rig.GetAsync();

        Assert.AreEqual("Valinhos", refreshed.City);
        CepCacheEntry entry = (await rig.CacheAsync()).Single();
        Assert.AreEqual("Valinhos", entry.City);
        Assert.AreEqual(3556206, entry.IbgeCode);
        Assert.AreEqual(rig.Clock.Now.UtcDateTime, entry.FetchedAt);
    }

    [TestMethod]
    public async Task CepInexistente_NaoEntraNoCache()
    {
        using Rig rig = new();
        rig.Lookup.Respond = _ => null;

        await Assert.ThrowsExactlyAsync<NotFoundException>(() => rig.GetAsync("99999999"));

        Assert.IsEmpty(await rig.CacheAsync());
    }

    [TestMethod]
    public async Task ViaCepForaDoAr_ComEntradaVencida_Devolve503_NaoServeODadoVencido() // ADR-007 (D5)
    {
        using Rig rig = new();
        await rig.GetAsync();
        rig.Clock.Now = rig.Clock.Now.AddDays(45);
        rig.Lookup.Respond = _ => FakeCepLookup.Unavailable();

        await Assert.ThrowsExactlyAsync<ServiceUnavailableException>(() => rig.GetAsync());

        Assert.AreEqual("Campinas", (await rig.CacheAsync()).Single().City, "a entrada vencida fica como está");
    }

    [TestMethod]
    public async Task ViaCepForaDoAr_SemCache_NaoGravaNada()
    {
        using Rig rig = new();
        rig.Lookup.Respond = _ => FakeCepLookup.Unavailable();

        await Assert.ThrowsExactlyAsync<ServiceUnavailableException>(() => rig.GetAsync());

        Assert.IsEmpty(await rig.CacheAsync());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("1234567")]
    [DataRow("abcdefgh")]
    [DataRow(null)]
    public async Task CepMalformado_ValidationException_SemConsultarNemLerOBanco(string cep)
    {
        using Rig rig = new();

        ValidationException error = await Assert.ThrowsExactlyAsync<ValidationException>(() => rig.GetAsync(cep));

        Assert.AreEqual("VALIDATION_ERROR", error.Code);
        Assert.AreEqual(0, rig.Lookup.Calls);
    }

    [TestMethod]
    public async Task DuasPessoasConsultandoOMesmoCepAoMesmoTempo_UmaLinhaSo_AmbasRecebemResposta()
    {
        using Rig rig = new();
        // No meio da consulta de A, B grava o mesmo CEP: o INSERT de A bate na chave primária
        bool injected = false;
        rig.Lookup.Meanwhile = async _ =>
        {
            if (injected)
            {
                return;
            }

            injected = true;
            await rig.GetAsync(); // B termina inteira (consulta e grava) antes de A gravar
        };

        CepResult a = await rig.GetAsync();

        Assert.AreEqual("viacep", a.Source);
        Assert.AreEqual("Campinas", a.City);
        Assert.HasCount(1, await rig.CacheAsync());
    }

    [TestMethod]
    public async Task Cidade_ConferidaComAListaDoIbge_UsaONomeOficialEAUfDaLista()
    {
        using Rig rig = new();
        await rig.AddCityAsync(3550308, "São Paulo", "SP");
        rig.Lookup.Respond = _ => new CepLookupResult("sao paulo", "SP", 3550308);

        CepResult result = await rig.GetAsync();

        Assert.AreEqual("São Paulo", result.City, "o nome oficial, com acento e maiúsculas, vence o do ViaCEP");
        Assert.AreEqual("São Paulo", (await rig.CacheAsync()).Single().City, "o cache guarda o nome já padronizado");
    }

    [TestMethod]
    public async Task CidadeForaDaLista_OuSemCodigo_UsaARegraDaSpec()
    {
        using Rig rig = new();
        await rig.AddCityAsync(3550308, "São Paulo", "SP");
        rig.Lookup.Respond = cep => cep == Cep
            ? new CepLookupResult("SÃO JOSÉ DO RIO PRETO", "SP", 3549805) // código que não está na lista (carga parcial)
            : new CepLookupResult("sao jose", "SP", null); // ViaCEP sem código

        CepResult notListed = await rig.GetAsync();
        CepResult noCode = await rig.GetAsync("13015200");

        Assert.AreEqual("São José do Rio Preto", notListed.City, "iniciais maiúsculas, acentos mantidos, artigos minúsculos");
        Assert.AreEqual("Sao Jose", noCode.City, "sem acento no original, sem acento no resultado (a SPEC não inventa acentos)");
    }
}

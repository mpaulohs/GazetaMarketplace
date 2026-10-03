using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Conta;

/// <summary>S17 / ADR-003: o primeiro Administrador nasce das variáveis de ambiente, uma vez, e a senha nunca vai para o log.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class BootstrapAdminTests
#pragma warning restore CA1515
{
    // Ids fixos dos papéis (semeados pela migration); a configuração é interna à Infrastructure
    private const int IdAdministrador = 1;
    private const int IdRedator = 2;

    private const string Email = "chefe@exemplo.com.br";
    private const string Senha = "Inicial@Senha1";

    private static Dictionary<string, string> Variaveis(string email = Email, string password = Senha)
    {
        Dictionary<string, string> v = [];
        if (email is not null)
        {
            v["Bootstrap:AdminEmail"] = email;
        }

        if (password is not null)
        {
            v["Bootstrap:AdminPassword"] = password;
        }

        return v;
    }

    private static string TudoNoLog(WebFactory factory) => string.Join("\n", factory.Logs.Events.Select(CollectorSink.AllAsText));

    private static Exception ErroDaPartida(WebFactory factory)
    {
        try
        {
            factory.CreateClient().Dispose();
        }
        catch (Exception error)
        {
            return error;
        }

        Assert.Fail("A partida deveria ter falhado");
        return null;
    }

    private static bool EhBootstrapInvalido(Exception error) =>
        error is InvalidBootstrapException || error.InnerException is InvalidBootstrapException || (error is AggregateException a && a.InnerExceptions.Any(EhBootstrapInvalido));

    [TestMethod]
    public async Task SemAdministrador_ComVariaveis_CriaComTrocaObrigatoria()
    {
        using WebFactory factory = new(configuration: Variaveis(), withDatabase: true);
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        IReadOnlyList<UsuarioIdentity> users = await factory.ListUsersAsync();

        UsuarioIdentity admin = users.Single();
        Assert.AreEqual(Email, admin.Email);
        Assert.AreEqual("Administrador", admin.FullName);
        Assert.IsTrue(admin.MustChangePassword, "troca obrigatória no primeiro acesso");
        Assert.IsTrue(admin.IsActive);
        Assert.IsFalse(admin.PasswordHash.Contains(Senha, StringComparison.Ordinal));

        // Entra com a senha inicial e é levado à troca
        Assert.AreEqual("/painel/definir-senha", (await client.EntrarAsync(Email, Senha)).Destino());
    }

    [TestMethod]
    public async Task ComAdministrador_NaoCriaOutro()
    {
        using WebFactory factory = new(configuration: Variaveis("outro@exemplo.com.br", "Outra@Senha1"), withDatabase: true,
            seed: ctx => WebFactory.SeedUser(ctx, "existente@exemplo.com.br", IdAdministrador));

        IReadOnlyList<UsuarioIdentity> users = await factory.ListUsersAsync();

        Assert.AreEqual("existente@exemplo.com.br", users.Single().Email);
    }

    [TestMethod]
    public async Task ComAdministradorDesativado_TambemNaoCriaOutro()
    {
        using WebFactory factory = new(configuration: Variaveis(), withDatabase: true,
            seed: ctx => WebFactory.SeedUser(ctx, "desativado@exemplo.com.br", IdAdministrador, active: false));

        IReadOnlyList<UsuarioIdentity> users = await factory.ListUsersAsync();

        Assert.AreEqual(1, users.Count, "quem reativa um Administrador é outro Administrador, não esta rotina");
        Assert.IsFalse(users.Single().IsActive);
    }

    [TestMethod]
    public async Task ComSoRedatores_CriaOAdministrador()
    {
        using WebFactory factory = new(configuration: Variaveis(), withDatabase: true,
            seed: ctx => WebFactory.SeedUser(ctx, "redator@exemplo.com.br", IdRedator));

        IReadOnlyList<UsuarioIdentity> users = await factory.ListUsersAsync();

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.Any(u => u.Email == Email));
    }

    [TestMethod]
    public async Task VariaveisRemanescentes_RegistramWarning() // RC-19
    {
        using WebFactory factory = new(configuration: Variaveis(), withDatabase: true,
            seed: ctx => WebFactory.SeedUser(ctx, "existente@exemplo.com.br", IdAdministrador));

        await factory.ListUsersAsync();

        LogEvent aviso = factory.Logs.Events.Single(e => e.Level == LogEventLevel.Warning && CollectorSink.Text(e).Contains("ainda existe", StringComparison.Ordinal));
        string text = CollectorSink.Text(aviso);
        StringAssert.Contains(text, "Bootstrap__AdminEmail");
        StringAssert.Contains(text, "Bootstrap__AdminPassword");
        StringAssert.Contains(text, "Remova");
        Assert.IsFalse(TudoNoLog(factory).Contains(Senha, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SoUmaVariavelRemanescente_ComAdministrador_ApenasAvisa()
    {
        using WebFactory factory = new(configuration: Variaveis(password: null), withDatabase: true,
            seed: ctx => WebFactory.SeedUser(ctx, "existente@exemplo.com.br", IdAdministrador));

        Assert.AreEqual(1, (await factory.ListUsersAsync()).Count, "não derruba nem cria");
        StringAssert.Contains(string.Join("\n", factory.Logs.Events.Select(CollectorSink.Text)), "Bootstrap__AdminEmail");
    }

    [TestMethod]
    public async Task SenhaInicial_NaoApareceNoLog()
    {
        using WebFactory factory = new(configuration: Variaveis(), withDatabase: true);

        await factory.ListUsersAsync();

        string all = TudoNoLog(factory);
        Assert.IsFalse(all.Contains(Senha, StringComparison.Ordinal), "a senha inicial foi para o log");
        Assert.IsFalse(all.Contains(Email, StringComparison.Ordinal), "o e-mail completo foi para o log");
        StringAssert.Contains(all, "Administrador inicial criado");
        StringAssert.Contains(all, "c***@exemplo.com.br");
    }

    [TestMethod]
    public async Task RodarDuasVezes_CriaUmaContaSo_ERegistraOAviso()
    {
        using WebFactory factory = new(configuration: Variaveis(), withDatabase: true);
        Assert.AreEqual(1, (await factory.ListUsersAsync()).Count);

        BootstrapAdminInitializer rotina = factory.Services.GetServices<IHostedService>().OfType<BootstrapAdminInitializer>().Single();
        await rotina.StartAsync(default);

        Assert.AreEqual(1, (await factory.ListUsersAsync()).Count);
        Assert.IsTrue(factory.Logs.Events.Any(e => e.Level == LogEventLevel.Warning && CollectorSink.Text(e).Contains("ainda existe", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task SemVariaveis_NaoTocaNoBanco_NemDerrubaAPartida()
    {
        using WebFactory factory = new(); // sem banco algum: qualquer consulta viraria Error no log

        using HttpClient client = factory.CreateClient();

        Assert.IsFalse(factory.Logs.Events.Any(e => e.Level >= LogEventLevel.Error), "nenhum erro na partida");
        await Task.CompletedTask;
    }

    [TestMethod]
    public void SenhaQueNaoCumpreAPolitica_DerrubaAPartida_SemRepetirASenha()
    {
        using WebFactory factory = new(configuration: Variaveis(password: "fraca1"), withDatabase: true);

        Exception error = ErroDaPartida(factory);

        Assert.IsTrue(EhBootstrapInvalido(error), error.ToString());
        string message = error.ToString();
        StringAssert.Contains(message, "Bootstrap__AdminPassword");
        Assert.IsFalse(message.Contains("fraca1", StringComparison.Ordinal), "a mensagem não repete a senha");
    }

    [TestMethod]
    public void EmailInvalido_DerrubaAPartida()
    {
        using WebFactory factory = new(configuration: Variaveis("isto-nao-e-email"), withDatabase: true);

        Exception error = ErroDaPartida(factory);

        Assert.IsTrue(EhBootstrapInvalido(error), error.ToString());
        StringAssert.Contains(error.ToString(), "Bootstrap__AdminEmail");
    }

    [TestMethod]
    public void SoUmaVariavel_SemAdministrador_DerrubaAPartida()
    {
        using WebFactory factory = new(configuration: Variaveis(password: null), withDatabase: true);

        Exception error = ErroDaPartida(factory);

        Assert.IsTrue(EhBootstrapInvalido(error), error.ToString());
        StringAssert.Contains(error.ToString(), "precisam existir juntas");
    }

    [TestMethod]
    public async Task BancoIndisponivel_RegistraErro_EOSiteSobe()
    {
        using WebFactory factory = new(configuration: Variaveis()); // sem banco de teste: a consulta falha

        using HttpClient client = factory.CreateClient();

        Assert.IsTrue(factory.Logs.Events.Any(e => e.Level == LogEventLevel.Error && CollectorSink.Text(e).Contains("Administrador", StringComparison.Ordinal)), "o erro fica no log");
        Assert.IsFalse(TudoNoLog(factory).Contains(Senha, StringComparison.Ordinal));
        await Task.CompletedTask;
    }
}

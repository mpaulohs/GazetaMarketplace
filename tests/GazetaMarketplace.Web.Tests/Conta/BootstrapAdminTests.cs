using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Tests.Suporte;
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

    private static Dictionary<string, string> Variaveis(string email = Email, string senha = Senha)
    {
        Dictionary<string, string> v = [];
        if (email is not null)
        {
            v["Bootstrap:AdminEmail"] = email;
        }

        if (senha is not null)
        {
            v["Bootstrap:AdminPassword"] = senha;
        }

        return v;
    }

    private static string TudoNoLog(FabricaWeb fabrica) => string.Join("\n", fabrica.Logs.Eventos.Select(ColetorSink.TudoComoTexto));

    private static Exception ErroDaPartida(FabricaWeb fabrica)
    {
        try
        {
            fabrica.CreateClient().Dispose();
        }
        catch (Exception erro)
        {
            return erro;
        }

        Assert.Fail("A partida deveria ter falhado");
        return null;
    }

    private static bool EhBootstrapInvalido(Exception erro) =>
        erro is BootstrapInvalidoException || erro.InnerException is BootstrapInvalidoException || (erro is AggregateException a && a.InnerExceptions.Any(EhBootstrapInvalido));

    [TestMethod]
    public async Task SemAdministrador_ComVariaveis_CriaComTrocaObrigatoria()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(), comBanco: true);
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);

        IReadOnlyList<UsuarioIdentity> usuarios = await fabrica.ListarUsuariosAsync();

        UsuarioIdentity admin = usuarios.Single();
        Assert.AreEqual(Email, admin.Email);
        Assert.AreEqual("Administrador", admin.FullName);
        Assert.IsTrue(admin.MustChangePassword, "troca obrigatória no primeiro acesso");
        Assert.IsTrue(admin.IsActive);
        Assert.IsFalse(admin.PasswordHash.Contains(Senha, StringComparison.Ordinal));

        // Entra com a senha inicial e é levado à troca
        Assert.AreEqual("/painel/definir-senha", (await cliente.EntrarAsync(Email, Senha)).Destino());
    }

    [TestMethod]
    public async Task ComAdministrador_NaoCriaOutro()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis("outro@exemplo.com.br", "Outra@Senha1"), comBanco: true,
            semear: ctx => FabricaWeb.SemearUsuario(ctx, "existente@exemplo.com.br", IdAdministrador));

        IReadOnlyList<UsuarioIdentity> usuarios = await fabrica.ListarUsuariosAsync();

        Assert.AreEqual("existente@exemplo.com.br", usuarios.Single().Email);
    }

    [TestMethod]
    public async Task ComAdministradorDesativado_TambemNaoCriaOutro()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(), comBanco: true,
            semear: ctx => FabricaWeb.SemearUsuario(ctx, "desativado@exemplo.com.br", IdAdministrador, ativo: false));

        IReadOnlyList<UsuarioIdentity> usuarios = await fabrica.ListarUsuariosAsync();

        Assert.AreEqual(1, usuarios.Count, "quem reativa um Administrador é outro Administrador, não esta rotina");
        Assert.IsFalse(usuarios.Single().IsActive);
    }

    [TestMethod]
    public async Task ComSoRedatores_CriaOAdministrador()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(), comBanco: true,
            semear: ctx => FabricaWeb.SemearUsuario(ctx, "redator@exemplo.com.br", IdRedator));

        IReadOnlyList<UsuarioIdentity> usuarios = await fabrica.ListarUsuariosAsync();

        Assert.AreEqual(2, usuarios.Count);
        Assert.IsTrue(usuarios.Any(u => u.Email == Email));
    }

    [TestMethod]
    public async Task VariaveisRemanescentes_RegistramWarning() // RC-19
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(), comBanco: true,
            semear: ctx => FabricaWeb.SemearUsuario(ctx, "existente@exemplo.com.br", IdAdministrador));

        await fabrica.ListarUsuariosAsync();

        LogEvent aviso = fabrica.Logs.Eventos.Single(e => e.Level == LogEventLevel.Warning && ColetorSink.Texto(e).Contains("ainda existe", StringComparison.Ordinal));
        string texto = ColetorSink.Texto(aviso);
        StringAssert.Contains(texto, "Bootstrap__AdminEmail");
        StringAssert.Contains(texto, "Bootstrap__AdminPassword");
        StringAssert.Contains(texto, "Remova");
        Assert.IsFalse(TudoNoLog(fabrica).Contains(Senha, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SoUmaVariavelRemanescente_ComAdministrador_ApenasAvisa()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(senha: null), comBanco: true,
            semear: ctx => FabricaWeb.SemearUsuario(ctx, "existente@exemplo.com.br", IdAdministrador));

        Assert.AreEqual(1, (await fabrica.ListarUsuariosAsync()).Count, "não derruba nem cria");
        StringAssert.Contains(string.Join("\n", fabrica.Logs.Eventos.Select(ColetorSink.Texto)), "Bootstrap__AdminEmail");
    }

    [TestMethod]
    public async Task SenhaInicial_NaoApareceNoLog()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(), comBanco: true);

        await fabrica.ListarUsuariosAsync();

        string tudo = TudoNoLog(fabrica);
        Assert.IsFalse(tudo.Contains(Senha, StringComparison.Ordinal), "a senha inicial foi para o log");
        Assert.IsFalse(tudo.Contains(Email, StringComparison.Ordinal), "o e-mail completo foi para o log");
        StringAssert.Contains(tudo, "Administrador inicial criado");
        StringAssert.Contains(tudo, "c***@exemplo.com.br");
    }

    [TestMethod]
    public async Task RodarDuasVezes_CriaUmaContaSo_ERegistraOAviso()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(), comBanco: true);
        Assert.AreEqual(1, (await fabrica.ListarUsuariosAsync()).Count);

        BootstrapAdminInitializer rotina = fabrica.Services.GetServices<IHostedService>().OfType<BootstrapAdminInitializer>().Single();
        await rotina.StartAsync(default);

        Assert.AreEqual(1, (await fabrica.ListarUsuariosAsync()).Count);
        Assert.IsTrue(fabrica.Logs.Eventos.Any(e => e.Level == LogEventLevel.Warning && ColetorSink.Texto(e).Contains("ainda existe", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task SemVariaveis_NaoTocaNoBanco_NemDerrubaAPartida()
    {
        using FabricaWeb fabrica = new(); // sem banco algum: qualquer consulta viraria Error no log

        using HttpClient cliente = fabrica.CreateClient();

        Assert.IsFalse(fabrica.Logs.Eventos.Any(e => e.Level >= LogEventLevel.Error), "nenhum erro na partida");
        await Task.CompletedTask;
    }

    [TestMethod]
    public void SenhaQueNaoCumpreAPolitica_DerrubaAPartida_SemRepetirASenha()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(senha: "fraca1"), comBanco: true);

        Exception erro = ErroDaPartida(fabrica);

        Assert.IsTrue(EhBootstrapInvalido(erro), erro.ToString());
        string mensagem = erro.ToString();
        StringAssert.Contains(mensagem, "Bootstrap__AdminPassword");
        Assert.IsFalse(mensagem.Contains("fraca1", StringComparison.Ordinal), "a mensagem não repete a senha");
    }

    [TestMethod]
    public void EmailInvalido_DerrubaAPartida()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis("isto-nao-e-email"), comBanco: true);

        Exception erro = ErroDaPartida(fabrica);

        Assert.IsTrue(EhBootstrapInvalido(erro), erro.ToString());
        StringAssert.Contains(erro.ToString(), "Bootstrap__AdminEmail");
    }

    [TestMethod]
    public void SoUmaVariavel_SemAdministrador_DerrubaAPartida()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis(senha: null), comBanco: true);

        Exception erro = ErroDaPartida(fabrica);

        Assert.IsTrue(EhBootstrapInvalido(erro), erro.ToString());
        StringAssert.Contains(erro.ToString(), "precisam existir juntas");
    }

    [TestMethod]
    public async Task BancoIndisponivel_RegistraErro_EOSiteSobe()
    {
        using FabricaWeb fabrica = new(configuracao: Variaveis()); // sem banco de teste: a consulta falha

        using HttpClient cliente = fabrica.CreateClient();

        Assert.IsTrue(fabrica.Logs.Eventos.Any(e => e.Level == LogEventLevel.Error && ColetorSink.Texto(e).Contains("Administrador", StringComparison.Ordinal)), "o erro fica no log");
        Assert.IsFalse(TudoNoLog(fabrica).Contains(Senha, StringComparison.Ordinal));
        await Task.CompletedTask;
    }
}

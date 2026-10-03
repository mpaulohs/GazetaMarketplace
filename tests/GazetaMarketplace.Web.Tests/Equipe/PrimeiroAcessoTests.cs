using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Web.Areas.Painel.Controllers;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Equipe;

/// <summary>US-006-S09: a senha provisória obriga a trocar antes de ver o painel.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PrimeiroAcessoTests
#pragma warning restore CA1515
{
    private const string Email = "nova@exemplo.com.br";
    private const string Provisoria = "Provisoria@1";
    private const string Nova = "Nova@Senha2";

    private static async Task<FabricaWeb> NovaFabricaAsync(string papel = Papeis.Redator)
    {
        FabricaWeb fabrica = new(comBanco: true);
        await fabrica.CriarUsuarioAsync(Email, "Nova Pessoa", Provisoria, papel, trocarSenha: true);
        await fabrica.CriarUsuarioAsync("veterana@exemplo.com.br", "Veterana", "Senha@Forte1", Papeis.Redator);
        return fabrica;
    }

    private static async Task<bool> PrecisaTrocarAsync(FabricaWeb fabrica) =>
        (await fabrica.ListarUsuariosAsync()).Single(u => u.Email == Email).MustChangePassword;

    [TestMethod]
    public async Task US006S09_PrimeiroAcessoExigeTrocarASenhaProvisoria() // @US-006-S09
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);

        // Entra com a senha provisória e cai na troca, antes de ver o painel
        HttpResponseMessage entrada = await cliente.EntrarAsync(Email, Provisoria);
        Assert.AreEqual("/painel/definir-senha", entrada.Destino());
        HttpResponseMessage tela = await cliente.GetAsync("/painel/definir-senha");
        string html = await tela.TextoAsync();
        Assert.AreEqual(HttpStatusCode.OK, tela.StatusCode);
        StringAssert.Contains(html, "Defina sua nova senha");
        StringAssert.Contains(html, "Você entrou com uma senha provisória.");
        StringAssert.Contains(html, "A senha precisa ter 8 caracteres ou mais, maiúscula, minúscula, número e símbolo.");

        // Define e confirma uma senha que cumpre a política: vê o painel
        HttpResponseMessage troca = await cliente.DefinirSenhaAsync(Nova);
        Assert.AreEqual(HttpStatusCode.Redirect, troca.StatusCode);
        Assert.AreEqual("/painel/anuncios", troca.Destino());
        StringAssert.Contains(await (await cliente.GetAsync("/painel/anuncios")).TextoAsync(), "Meus anúncios");
        Assert.IsFalse(await PrecisaTrocarAsync(fabrica));

        // A senha provisória deixou de valer; a nova entra direto
        using HttpClient outro = ClienteDaEquipe.Novo(fabrica);
        StringAssert.Contains(await (await outro.EntrarAsync(Email, Provisoria)).TextoAsync(), ContaController.MensagemDeFalha);
        Assert.AreEqual("/painel/anuncios", (await outro.EntrarAsync(Email, Nova)).Destino());
    }

    [TestMethod]
    public async Task EnquantoASenhaForProvisoria_TodaPaginaDoPainelLevaATroca()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync(Papeis.Administrador);
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Provisoria);

        foreach (string pagina in new[] { "/painel/anuncios", "/painel/anuncios/fila" })
        {
            HttpResponseMessage resposta = await cliente.GetAsync(pagina);
            Assert.AreEqual(HttpStatusCode.Redirect, resposta.StatusCode, pagina);
            Assert.AreEqual("/painel/definir-senha", resposta.Destino(), pagina);
        }

        Assert.AreEqual(HttpStatusCode.OK, (await cliente.GetAsync("/painel/definir-senha")).StatusCode, "a própria tela de troca não entra em laço");
    }

    [TestMethod]
    public async Task DuranteATroca_OMenuFicaOculto_ESairContinuaDisponivel()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync(Papeis.Administrador);
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Provisoria);

        string html = await (await cliente.GetAsync("/painel/definir-senha")).TextoAsync();
        foreach (string rota in new[] { "/painel/anuncios", "/painel/categorias", "/painel/usuarios", "/painel/configuracoes" })
        {
            Assert.IsFalse(html.Contains("href=\"" + rota + "\"", StringComparison.Ordinal), "sem menu: " + rota);
        }

        StringAssert.Matches(html, new Regex(@"<button[^>]*>(?:\s*<i[^>]*></i>)?\s*Sair\s*</button>"));
        HttpResponseMessage saida = await cliente.SairAsync("/painel/definir-senha");
        Assert.AreEqual("/painel/entrar", saida.Destino());
    }

    [TestMethod]
    public async Task SenhaFraca_E_Recusada_ESenhaProvisoriaContinuaValendo()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Provisoria);

        (string Fraca, string Mensagem)[] casos =
        [
            ("Ab1!", "A senha precisa ter 8 caracteres ou mais."),
            ("senha@forte1", "A senha precisa ter uma letra maiúscula."),
            ("SENHA@FORTE1", "A senha precisa ter uma letra minúscula."),
            ("Senha@Forte", "A senha precisa ter um número."),
            ("SenhaForte12", "A senha precisa ter um símbolo")
        ];
        foreach ((string fraca, string mensagem) in casos)
        {
            HttpResponseMessage resposta = await cliente.DefinirSenhaAsync(fraca);
            string html = await resposta.TextoAsync();
            Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode, fraca);
            StringAssert.Contains(html, mensagem, fraca);
            Assert.IsFalse(html.Contains(fraca, StringComparison.Ordinal), "a senha digitada não volta para a tela: " + fraca);
        }

        // Atômico: nenhuma tentativa recusada pode ter apagado ou trocado a senha provisória
        Assert.IsTrue(await PrecisaTrocarAsync(fabrica));
        using HttpClient outro = ClienteDaEquipe.Novo(fabrica);
        Assert.AreEqual("/painel/definir-senha", (await outro.EntrarAsync(Email, Provisoria)).Destino(), "a provisória ainda entra");
    }

    [TestMethod]
    public async Task NovaSenhaIgualAProvisoria_E_Recusada()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Provisoria);

        string html = await (await cliente.DefinirSenhaAsync(Provisoria)).TextoAsync();

        StringAssert.Contains(html, SenhaController.MensagemIgualAProvisoria);
        Assert.IsTrue(await PrecisaTrocarAsync(fabrica));
    }

    [TestMethod]
    public async Task ConfirmacaoDiferente_ECamposEmBranco_MostramMensagemPorCampo()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Provisoria);

        StringAssert.Contains(await (await cliente.DefinirSenhaAsync(Nova, "Outra@Senha3")).TextoAsync(), "As senhas não são iguais.");
        string vazio = await (await cliente.DefinirSenhaAsync(string.Empty, string.Empty)).TextoAsync();
        StringAssert.Contains(vazio, "Informe a nova senha.");
        StringAssert.Contains(vazio, "Confirme a nova senha.");
        Assert.IsTrue(await PrecisaTrocarAsync(fabrica));
    }

    [TestMethod]
    public async Task Administrador_DepoisDaTroca_VaiParaAFila()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync(Papeis.Administrador);
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Provisoria);

        HttpResponseMessage troca = await cliente.DefinirSenhaAsync(Nova);

        Assert.AreEqual("/painel/anuncios/fila", troca.Destino());
    }

    [TestMethod]
    public async Task QuemNaoPrecisaTrocar_NaoVeATela_EQuemNaoEntrouVaiParaAEntrada()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient veterana = ClienteDaEquipe.Novo(fabrica);
        await veterana.EntrarAsync("veterana@exemplo.com.br", "Senha@Forte1");
        HttpResponseMessage semPrecisar = await veterana.GetAsync("/painel/definir-senha");
        Assert.AreEqual("/painel/anuncios", semPrecisar.Destino());

        using HttpClient anonimo = ClienteDaEquipe.Novo(fabrica);
        HttpResponseMessage semLogin = await anonimo.GetAsync("/painel/definir-senha");
        StringAssert.StartsWith(semLogin.Destino(), "/painel/entrar");
    }

    [TestMethod]
    public async Task SenhasDaTroca_NaoVaoParaOLog()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Provisoria);
        await cliente.DefinirSenhaAsync("fraca");
        await cliente.DefinirSenhaAsync(Nova);

        string tudo = string.Join("\n", fabrica.Logs.Eventos.Select(ColetorSink.TudoComoTexto));
        foreach (string segredo in new[] { Provisoria, Nova, "fraca" })
        {
            Assert.IsFalse(tudo.Contains(segredo, StringComparison.Ordinal), "a senha " + segredo + " foi para o log");
        }

        StringAssert.Contains(tudo, "definiu a nova senha");
    }
}

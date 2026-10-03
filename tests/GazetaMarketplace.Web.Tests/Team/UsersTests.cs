using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Areas.Panel.Controllers;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Team;

/// <summary>US-014: gestão das contas da equipe pelo Administrador (S01 a S10) e a barreira de acesso da US-006-S10.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class UsersTests
#pragma warning restore CA1515
{
    private const string AdminEmail = "marcos@exemplo.com.br";
    private const string AnaEmail = "ana.souza@exemplo.com.br";
    private const string Password = "Senha@Forte1";
    private const string Provisional = "Provis0ria!";

    private static async Task<WebFactory> NewFactoryAsync(bool withAna = false)
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(AdminEmail, "Marcos Silva", Password, RoleNames.Administrator);
        if (withAna)
        {
            await factory.CreateUserAsync(AnaEmail, "Ana Souza", Password, RoleNames.Writer);
        }

        return factory;
    }

    private static async Task<HttpClient> AdminClientAsync(WebFactory factory, string email = AdminEmail)
    {
        HttpClient client = TeamClient.Create(factory);
        HttpResponseMessage entry = await client.SignInAsync(email, Password);
        Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode, "a conta de teste precisa entrar");
        return client;
    }

    private static async Task<AppUser> FindAsync(WebFactory factory, string email) =>
        (await factory.ListUsersAsync()).SingleOrDefault(u => u.Email == email);

    private static Dictionary<string, string> NewUserFields(
        string name = "Ana Souza", string email = AnaEmail, string role = RoleNames.Writer, string password = Provisional) =>
        new() { ["FullName"] = name, ["Email"] = email, ["Role"] = role, ["ProvisionalPassword"] = password };

    private static async Task<HttpResponseMessage> CreateAsync(HttpClient client, Dictionary<string, string> fields) =>
        await client.PostFormAsync("/painel/usuarios/novo", "/painel/usuarios/novo", fields);

    private static Task<HttpResponseMessage> ChangeRoleAsync(HttpClient client, int id, string role) =>
        client.PostFormAsync($"/painel/usuarios/{id}/editar", $"/painel/usuarios/{id}/editar", new Dictionary<string, string> { ["Role"] = role });

    private static Task<HttpResponseMessage> DeactivateAsync(HttpClient client, int id) =>
        client.PostFormAsync($"/painel/usuarios/{id}/desativar", $"/painel/usuarios/{id}/desativar");

    private static Task<HttpResponseMessage> ResetPasswordAsync(HttpClient client, int id, string password) =>
        client.PostFormAsync(
            $"/painel/usuarios/{id}/redefinir-senha",
            $"/painel/usuarios/{id}/redefinir-senha",
            new Dictionary<string, string> { ["ProvisionalPassword"] = password });

    /// <summary>A linha da pessoa na tabela da lista: nome, e-mail, papel e situação, nesta ordem.</summary>
    private static string Row(string html, string name)
    {
        Match match = Regex.Match(html, @"<tr>\s*<td>" + Regex.Escape(name) + @"</td>(?<cells>[\s\S]*?)</tr>");
        Assert.IsTrue(match.Success, $"a lista não mostra {name}");
        return Regex.Replace(match.Groups["cells"].Value, @"<[^>]+>", " ");
    }

    private static async Task<string> ListAsync(HttpClient client) =>
        await (await client.GetAsync("/painel/usuarios")).TextAsync();

    [TestMethod]
    public async Task US014S01_CriarUmaContaDeRedator() // @US-014-S01
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await AdminClientAsync(factory);

        HttpResponseMessage created = await CreateAsync(admin, NewUserFields());
        Assert.AreEqual(HttpStatusCode.Redirect, created.StatusCode);
        Assert.AreEqual("/painel/usuarios", created.Destination());

        string html = await ListAsync(admin);
        StringAssert.Contains(html, "Usuário Ana Souza criado.");
        string row = Row(html, "Ana Souza");
        StringAssert.Contains(row, AnaEmail);
        StringAssert.Contains(row, "Redator");
        StringAssert.Contains(row, "Ativa");

        AppUser ana = await FindAsync(factory, AnaEmail);
        Assert.IsTrue(ana.IsActive);
        Assert.IsTrue(ana.MustChangePassword, "a conta nasce com troca de senha obrigatória");

        // Ana entra com a senha provisória e é levada a definir uma nova senha antes de ver o painel
        using HttpClient anaClient = TeamClient.Create(factory);
        HttpResponseMessage entry = await anaClient.SignInAsync(AnaEmail, Provisional);
        Assert.AreEqual("/painel/definir-senha", entry.Destination());
        Assert.AreEqual("/painel/definir-senha", (await anaClient.GetAsync("/painel/anuncios")).Destination());
    }

    [TestMethod]
    public async Task US014S02_MudarOPapelDeUmUsuario() // @US-014-S02
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser ana = await FindAsync(factory, AnaEmail);

        HttpResponseMessage changed = await ChangeRoleAsync(admin, ana.Id, RoleNames.Administrator);
        Assert.AreEqual("/painel/usuarios", changed.Destination());

        string html = await ListAsync(admin);
        StringAssert.Contains(Row(html, "Ana Souza"), "Administrador");

        // Na próxima vez que entra, Ana vê os menus de Administrador
        using HttpClient anaClient = await AdminClientAsync(factory, AnaEmail);
        string panel = await (await anaClient.GetAsync("/painel/anuncios")).TextAsync();
        foreach (string menu in new[] { "/painel/categorias", "/painel/usuarios", "/painel/configuracoes" })
        {
            StringAssert.Contains(panel, "href=\"" + menu + "\"");
        }
    }

    [TestMethod]
    public async Task US014S03_DesativarUmaConta() // @US-014-S03
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser ana = await FindAsync(factory, AnaEmail);

        string confirmation = await (await admin.GetAsync($"/painel/usuarios/{ana.Id}/desativar")).TextAsync();
        StringAssert.Contains(confirmation, "Desativar Ana Souza?");

        Assert.AreEqual("/painel/usuarios", (await DeactivateAsync(admin, ana.Id)).Destination());
        string html = await ListAsync(admin);
        StringAssert.Contains(html, "A conta de Ana Souza foi desativada.");
        StringAssert.Contains(Row(html, "Ana Souza"), "Desativada");

        // Não há exclusão: o cadastro (e, nas próximas tarefas, a autoria dos anúncios) continua com o nome dela
        Assert.AreEqual("Ana Souza", (await FindAsync(factory, AnaEmail)).FullName);

        using HttpClient anaClient = TeamClient.Create(factory);
        string failure = await (await anaClient.SignInAsync(AnaEmail, Password)).TextAsync();
        StringAssert.Contains(failure, AccountController.InvalidCredentialsMessage);
    }

    [TestMethod]
    public async Task US014S03_SessaoAbertaDaContaDesativada_AcabaEmAteCincoMinutos() // NFR-08
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient admin = await AdminClientAsync(factory);
        using HttpClient anaClient = await AdminClientAsync(factory, AnaEmail);
        Assert.AreEqual(HttpStatusCode.OK, (await anaClient.GetAsync("/painel/anuncios")).StatusCode);

        await DeactivateAsync(admin, (await FindAsync(factory, AnaEmail)).Id);

        factory.Clock.Now += TimeSpan.FromMinutes(6);
        Assert.AreEqual(HttpStatusCode.Redirect, (await anaClient.GetAsync("/painel/anuncios")).StatusCode, "passada a revalidação, a sessão acaba");
    }

    [TestMethod]
    public async Task US014S04_ReativarUmaConta() // @US-014-S04
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser ana = await FindAsync(factory, AnaEmail);
        await DeactivateAsync(admin, ana.Id);

        HttpResponseMessage reactivated = await admin.PostFormAsync("/painel/usuarios", $"/painel/usuarios/{ana.Id}/reativar");
        Assert.AreEqual("/painel/usuarios", reactivated.Destination());

        string html = await ListAsync(admin);
        StringAssert.Contains(html, "A conta de Ana Souza foi reativada.");
        StringAssert.Contains(Row(html, "Ana Souza"), "Ativa");

        using HttpClient anaClient = TeamClient.Create(factory);
        Assert.AreEqual("/painel/anuncios", (await anaClient.SignInAsync(AnaEmail, Password)).Destination());
    }

    [TestMethod]
    public async Task US014S05_EMailJaCadastrado() // @US-014-S05
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient admin = await AdminClientAsync(factory);
        int before = (await factory.ListUsersAsync()).Count;

        // Maiúsculas não escapam da regra: o e-mail é único sem diferenciar caixa
        HttpResponseMessage response = await CreateAsync(admin, NewUserFields(name: "Outra Ana", email: AnaEmail.ToUpperInvariant()));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string html = await response.TextAsync();
        Assert.AreEqual(1, Regex.Matches(html, Regex.Escape("Já existe um usuário com este e-mail")).Count, "a mensagem aparece uma vez só");
        Assert.AreEqual(before, (await factory.ListUsersAsync()).Count);
    }

    [TestMethod]
    public async Task US014S06_SenhaProvisoriaFraca() // @US-014-S06
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await AdminClientAsync(factory);

        HttpResponseMessage response = await CreateAsync(admin, NewUserFields(password: "12345"));

        string html = await response.TextAsync();
        foreach (string requirement in new[] { "8 caracteres", "letra maiúscula", "letra minúscula", "símbolo" })
        {
            StringAssert.Contains(html, requirement);
        }

        Assert.IsNull(await FindAsync(factory, AnaEmail), "o usuário não é criado");
        Assert.IsFalse(html.Contains("value=\"12345\"", StringComparison.Ordinal), "a senha digitada não volta para a tela");
    }

    [TestMethod]
    public async Task US014S07_EMailEmFormatoInvalido() // @US-014-S07
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await AdminClientAsync(factory);

        string html = await (await CreateAsync(admin, NewUserFields(email: "ana.souza"))).TextAsync();

        StringAssert.Contains(html, "Informe um e-mail válido");
        Assert.AreEqual(1, (await factory.ListUsersAsync()).Count, "só o Administrador existe");
    }

    [TestMethod]
    public async Task US014S08_DesativarAPropriaConta() // @US-014-S08
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser self = await FindAsync(factory, AdminEmail);

        HttpResponseMessage response = await DeactivateAsync(admin, self.Id);

        StringAssert.Contains(await response.TextAsync(), "Você não pode desativar a sua própria conta");
        Assert.IsTrue((await FindAsync(factory, AdminEmail)).IsActive, "a conta continua ativa");
        StringAssert.Contains(Row(await ListAsync(admin), "Marcos Silva"), "Ativa");
    }

    [TestMethod]
    public async Task US014S09_RemoverOUltimoAdministrador() // @US-014-S09
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser self = await FindAsync(factory, AdminEmail);

        HttpResponseMessage response = await ChangeRoleAsync(admin, self.Id, RoleNames.Writer);

        string html = await response.TextAsync();
        StringAssert.Contains(html, "Deve existir ao menos um administrador ativo");
        StringAssert.Contains(html, "Marcos Silva", "o formulário não perde o nome");
        StringAssert.Contains(Row(await ListAsync(admin), "Marcos Silva"), "Administrador");
    }

    [TestMethod]
    public async Task US014S09_ComDoisAdministradoresAtivos_UmPodeSerRebaixado_MasAContaDesativadaNaoConta()
    {
        using WebFactory factory = await NewFactoryAsync();
        await factory.CreateUserAsync("bruno@exemplo.com.br", "Bruno Lima", Password, RoleNames.Administrator);
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser bruno = await FindAsync(factory, "bruno@exemplo.com.br");

        Assert.AreEqual("/painel/usuarios", (await ChangeRoleAsync(admin, bruno.Id, RoleNames.Writer)).Destination(), "com dois ativos, um pode virar Redator");

        // Agora só Marcos é Administrador ativo: o rebaixado não pode voltar a Administrador e sair, nem Marcos se rebaixar
        AppUser marcos = await FindAsync(factory, AdminEmail);
        StringAssert.Contains(await (await ChangeRoleAsync(admin, marcos.Id, RoleNames.Writer)).TextAsync(), "Deve existir ao menos um administrador ativo");
    }

    [TestMethod]
    public async Task US014S09_AdministradorDesativadoNaoContaComoAtivo()
    {
        using WebFactory factory = await NewFactoryAsync();
        await factory.CreateUserAsync("bruno@exemplo.com.br", "Bruno Lima", Password, RoleNames.Administrator, active: false);
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser marcos = await FindAsync(factory, AdminEmail);

        string html = await (await ChangeRoleAsync(admin, marcos.Id, RoleNames.Writer)).TextAsync();

        StringAssert.Contains(html, "Deve existir ao menos um administrador ativo");
    }

    [TestMethod]
    public async Task US014S09_AdministradorDesativadoComSessaoAindaAberta_NaoDesativaOUltimoAdministrador()
    {
        using WebFactory factory = await NewFactoryAsync();
        await factory.CreateUserAsync("bruno@exemplo.com.br", "Bruno Lima", Password, RoleNames.Administrator);
        using HttpClient marcos = await AdminClientAsync(factory);
        using HttpClient bruno = await AdminClientAsync(factory, "bruno@exemplo.com.br");
        AppUser marcosUser = await FindAsync(factory, AdminEmail);
        AppUser brunoUser = await FindAsync(factory, "bruno@exemplo.com.br");

        // Bruno desativa Marcos, mas a sessão de Marcos vale até a revalidação (até 5 min): ele ainda tenta desativar Bruno
        await DeactivateAsync(bruno, marcosUser.Id);
        HttpResponseMessage attempt = await DeactivateAsync(marcos, brunoUser.Id);

        StringAssert.Contains(await attempt.TextAsync(), "Deve existir ao menos um administrador ativo");
        Assert.IsTrue((await FindAsync(factory, "bruno@exemplo.com.br")).IsActive, "o último Administrador ativo continua ativo");
    }

    [TestMethod]
    public async Task US014S10_RedefinirASenhaDeAlguemDaEquipe() // @US-014-S10
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient admin = await AdminClientAsync(factory);
        using HttpClient anaSession = await AdminClientAsync(factory, AnaEmail);
        AppUser ana = await FindAsync(factory, AnaEmail);

        HttpResponseMessage reset = await ResetPasswordAsync(admin, ana.Id, Provisional);
        Assert.AreEqual("/painel/usuarios", reset.Destination());
        StringAssert.Contains(await ListAsync(admin), "Senha de Ana Souza redefinida. Informe a senha provisória a ela fora do sistema.");

        // A senha anterior deixa de funcionar e a sessão aberta é encerrada
        using HttpClient oldPassword = TeamClient.Create(factory);
        StringAssert.Contains(await (await oldPassword.SignInAsync(AnaEmail, Password)).TextAsync(), AccountController.InvalidCredentialsMessage);
        factory.Clock.Now += TimeSpan.FromMinutes(6);
        Assert.AreEqual(HttpStatusCode.Redirect, (await anaSession.GetAsync("/painel/anuncios")).StatusCode, "as sessões abertas dela acabam");

        // Com a provisória ela é levada a definir uma nova senha antes do painel
        using HttpClient anaClient = TeamClient.Create(factory);
        Assert.AreEqual("/painel/definir-senha", (await anaClient.SignInAsync(AnaEmail, Provisional)).Destination());
        Assert.IsTrue((await FindAsync(factory, AnaEmail)).MustChangePassword);
    }

    [TestMethod]
    public async Task US014S10_SenhaFracaNaRedefinicao_MostraOsRequisitos_EASenhaAtualContinuaValendo()
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser ana = await FindAsync(factory, AnaEmail);

        string html = await (await ResetPasswordAsync(admin, ana.Id, "12345")).TextAsync();

        foreach (string requirement in new[] { "8 caracteres", "letra maiúscula", "letra minúscula", "símbolo" })
        {
            StringAssert.Contains(html, requirement);
        }

        using HttpClient anaClient = TeamClient.Create(factory);
        Assert.AreEqual("/painel/anuncios", (await anaClient.SignInAsync(AnaEmail, Password)).Destination(), "a senha atual não foi trocada");
    }

    [TestMethod]
    public async Task RedefinirSenha_NaoApareceParaAPropriaPessoaNemParaContaDesativada_ENoServicoTambemERecusada()
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        await factory.CreateUserAsync("bruno@exemplo.com.br", "Bruno Lima", Password, RoleNames.Writer, active: false);
        using HttpClient admin = await AdminClientAsync(factory);
        string html = await ListAsync(admin);

        Assert.IsFalse(html.Contains("aria-label=\"Redefinir senha de Marcos Silva\"", StringComparison.Ordinal), "a própria pessoa usa a recuperação normal");
        Assert.IsFalse(html.Contains("aria-label=\"Redefinir senha de Bruno Lima\"", StringComparison.Ordinal), "conta desativada não entra");
        StringAssert.Contains(html, "aria-label=\"Redefinir senha de Ana Souza\"");
        StringAssert.Contains(html, "aria-label=\"Reativar Bruno Lima\"");

        // Quem abre o endereço direto também é recusado
        AppUser marcos = await FindAsync(factory, AdminEmail);
        AppUser bruno = await FindAsync(factory, "bruno@exemplo.com.br");
        StringAssert.Contains(await (await ResetPasswordAsync(admin, marcos.Id, Provisional)).TextAsync(), "Para trocar a sua própria senha");
        StringAssert.Contains(await (await ResetPasswordAsync(admin, bruno.Id, Provisional)).TextAsync(), "Reative a conta antes de redefinir a senha");
    }

    [TestMethod]
    public async Task US006S10_RedatorTentaAbrirUmaPaginaExclusivaDoAdministrador() // @US-006-S10
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient writer = await AdminClientAsync(factory, AnaEmail);

        HttpResponseMessage response = await writer.GetAsync("/painel/usuarios");
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.StartsWith(response.Destination(), "/painel/acesso-negado");

        HttpResponseMessage denied = await writer.GetAsync(response.Destination());
        string html = await denied.TextAsync();
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        StringAssert.Contains(html, "Você não tem permissão para acessar esta página");
        Assert.IsFalse(html.Contains("Usuários da equipe", StringComparison.Ordinal), "o conteúdo da página não aparece");

        // Também não consegue agir por endereço direto
        AppUser marcos = await FindAsync(factory, AdminEmail);
        string panel = await writer.GetStringAsync("/painel/anuncios");
        string token = Regex.Match(panel, @"name=""request-verification-token""[^>]*content=""([^""]+)""").Groups[1].Value;
        using HttpRequestMessage attempt = new(HttpMethod.Post, $"/painel/usuarios/{marcos.Id}/editar")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["Role"] = RoleNames.Writer })
        };
        attempt.Headers.Add("RequestVerificationToken", token);
        HttpResponseMessage direct = await writer.SendAsync(attempt);
        Assert.AreEqual(HttpStatusCode.Redirect, direct.StatusCode);
        StringAssert.StartsWith(direct.Destination(), "/painel/acesso-negado");
        using HttpClient admin = await AdminClientAsync(factory);
        StringAssert.Contains(Row(await ListAsync(admin), "Marcos Silva"), "Administrador");
    }

    [TestMethod]
    public async Task SemLogin_AListaDeUsuariosLevaAEntrada()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient anonymous = TeamClient.Create(factory);

        HttpResponseMessage response = await anonymous.GetAsync("/painel/usuarios");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.StartsWith(response.Destination(), "/painel/entrar");
    }

    [TestMethod]
    public async Task NomeEmBranco_ENomeMuitoLongo_SaoRecusados()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await AdminClientAsync(factory);

        StringAssert.Contains(await (await CreateAsync(admin, NewUserFields(name: "  "))).TextAsync(), "Informe o nome");
        StringAssert.Contains(await (await CreateAsync(admin, NewUserFields(name: new string('A', 101)))).TextAsync(), "O nome pode ter até 100 caracteres");
        Assert.AreEqual(1, (await factory.ListUsersAsync()).Count);
    }

    [TestMethod]
    public async Task PapelFora_DosDoisExistentes_ERecusado()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await AdminClientAsync(factory);

        StringAssert.Contains(await (await CreateAsync(admin, NewUserFields(role: "Gerente"))).TextAsync(), "Escolha o papel");
        Assert.IsNull(await FindAsync(factory, AnaEmail));
    }

    [TestMethod]
    public async Task IdInexistente_Devolve404()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await AdminClientAsync(factory);

        Assert.AreEqual(HttpStatusCode.NotFound, (await admin.GetAsync("/painel/usuarios/999/editar")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await admin.GetAsync("/painel/usuarios/999/desativar")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await admin.GetAsync("/painel/usuarios/999/redefinir-senha")).StatusCode);
    }

    [TestMethod]
    public async Task AListaMostraTodos_PorOrdemDeNome_ComAcoesComONomeDaPessoa()
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        await factory.CreateUserAsync("bruno@exemplo.com.br", "Bruno Lima", Password, RoleNames.Writer, active: false);
        using HttpClient admin = await AdminClientAsync(factory);

        string html = await ListAsync(admin);
        string table = html[html.IndexOf("<tbody>", StringComparison.Ordinal)..html.IndexOf("</tbody>", StringComparison.Ordinal)];

        int ana = table.IndexOf("Ana Souza", StringComparison.Ordinal);
        int bruno = table.IndexOf("Bruno Lima", StringComparison.Ordinal);
        int marcos = table.IndexOf("Marcos Silva", StringComparison.Ordinal);
        Assert.IsTrue(ana >= 0 && ana < bruno && bruno < marcos, "ordem alfabética");
        StringAssert.Contains(html, "aria-label=\"Desativar Ana Souza\"");
        StringAssert.Contains(html, "aria-label=\"Editar Bruno Lima\"");
        StringAssert.Contains(Row(html, "Bruno Lima"), "Desativada");
    }

    [TestMethod]
    public async Task SemTokenAntiforgery_AEscritaERecusada()
    {
        using WebFactory factory = await NewFactoryAsync(withAna: true);
        using HttpClient admin = await AdminClientAsync(factory);
        AppUser ana = await FindAsync(factory, AnaEmail);

        using FormUrlEncodedContent form = new(new Dictionary<string, string>());
        HttpResponseMessage response = await admin.PostAsync($"/painel/usuarios/{ana.Id}/desativar", form);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsTrue((await FindAsync(factory, AnaEmail)).IsActive);
    }
}

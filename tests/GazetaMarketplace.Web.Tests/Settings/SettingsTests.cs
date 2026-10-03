using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Settings;

/// <summary>US-015: o Administrador configura o telefone/WhatsApp do site (S01 a S06), com auditoria (RC-16) e leitura pelo cache.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SettingsTests
#pragma warning restore CA1515
{
    private const string AdminEmail = "marcos@exemplo.com.br";
    private const string WriterEmail = "ana.souza@exemplo.com.br";
    private const string Password = "Senha@Forte1";
    private const string Page = "/painel/configuracoes";

    private static async Task<WebFactory> NewFactoryAsync()
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(AdminEmail, "Marcos Silva", Password, RoleNames.Administrator);
        await factory.CreateUserAsync(WriterEmail, "Ana Souza", Password, RoleNames.Writer);
        return factory;
    }

    private static async Task<HttpClient> SignedInAsync(WebFactory factory, string email = AdminEmail)
    {
        HttpClient client = TeamClient.Create(factory);
        HttpResponseMessage entry = await client.SignInAsync(email, Password);
        Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode, "a conta de teste precisa entrar");
        return client;
    }

    private static Task<HttpResponseMessage> SaveAsync(HttpClient client, string phone) =>
        client.PostFormAsync(Page, Page, new Dictionary<string, string> { ["Phone"] = phone });

    private static async Task<string> PhoneInUseAsync(WebFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISiteSettings>().GetPhoneAsync(CancellationToken.None);
    }

    private static async Task<List<AuditEntry>> AuditAsync(WebFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().AuditEntries.AsNoTracking()
            .Where(e => e.TargetType == "SiteSetting").OrderBy(e => e.Id).ToListAsync();
    }

    [TestMethod]
    public async Task US015S01_DefinirOTelefone_MostraAMensagem_EOSiteUsaONumero() // @US-015-S01
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);

        HttpResponseMessage saved = await SaveAsync(admin, "(11) 91234-5678");
        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode, "salvar redireciona (POST/redirecionar/GET)");

        string html = await (await admin.GetAsync(saved.Destination())).TextAsync();
        StringAssert.Contains(html, "Configurações salvas");
        StringAssert.Contains(html, "value=\"(11) 91234-5678\"");
        Assert.AreEqual("11912345678", await PhoneInUseAsync(factory), "o site lê o número guardado, só com dígitos");
    }

    [TestMethod]
    public async Task US015S02_TrocarONumero_OSiteVeOGeralNoMesmoInstante() // @US-015-S02
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);
        await SaveAsync(admin, "(11) 91234-5678");
        Assert.AreEqual("11912345678", await PhoneInUseAsync(factory), "o número antigo já foi lido (e está em cache)");

        await SaveAsync(admin, "(21) 98765-4321");

        Assert.AreEqual("21987654321", await PhoneInUseAsync(factory), "salvar invalida o cache: o novo número vale já, sem esperar os 10 minutos");
    }

    [TestMethod]
    [DataRow("11912345678", "11912345678")]
    [DataRow("+55 (11) 91234-5678", "11912345678")]
    [DataRow("5511912345678", "11912345678")]
    [DataRow("(11) 3456-7890", "1134567890")]
    public async Task US015S03_NumeroSemFormatacao_ESalvoEExibidoFormatado(string typed, string stored) // @US-015-S03
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);

        HttpResponseMessage saved = await SaveAsync(admin, typed);

        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.AreEqual(stored, await PhoneInUseAsync(factory));
        string html = await (await admin.GetAsync(Page)).TextAsync();
        StringAssert.Contains(html, $"value=\"{PhoneNumber.Format(stored)}\"");
    }

    [TestMethod]
    [DataRow("abc123")]
    [DataRow("(11) 81234-5678")]
    [DataRow("(00) 91234-5678")]
    [DataRow("+1 (11) 91234-5678")]
    public async Task US015S04_NumeroInvalido_MostraAMensagem_EONumeroEmUsoNaoMuda(string typed) // @US-015-S04
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);
        await SaveAsync(admin, "(11) 91234-5678");
        int auditBefore = (await AuditAsync(factory)).Count;

        HttpResponseMessage response = await SaveAsync(admin, typed);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "volta ao formulário, sem redirecionar");
        string html = await response.TextAsync();
        StringAssert.Contains(html, "Informe um número com DDD, por exemplo (11) 91234-5678");
        StringAssert.Contains(html, $"value=\"{typed}\"", "o que a pessoa digitou volta no campo");
        Assert.AreEqual("11912345678", await PhoneInUseAsync(factory));
        Assert.HasCount(auditBefore, await AuditAsync(factory), "recusar não deixa rastro na auditoria");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task US015S05_NumeroVazio_MostraAMensagem_EONumeroEmUsoNaoMuda(string typed) // @US-015-S05
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);
        await SaveAsync(admin, "(11) 91234-5678");

        HttpResponseMessage response = await SaveAsync(admin, typed);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.TextAsync(), "O telefone é obrigatório");
        Assert.AreEqual("11912345678", await PhoneInUseAsync(factory));
    }

    [TestMethod]
    public async Task US015S06_RedatorNaoAcessaAsConfiguracoes() // @US-015-S06
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient writer = await SignedInAsync(factory, WriterEmail);

        HttpResponseMessage response = await writer.GetAsync(Page);
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.StartsWith(response.Destination(), "/painel/acesso-negado");

        HttpResponseMessage denied = await writer.GetAsync(response.Destination());
        string html = await denied.TextAsync();
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        StringAssert.Contains(html, "Você não tem permissão para acessar esta página");
        Assert.DoesNotContain("name=\"Phone\"", html, "o campo do telefone não aparece");
    }

    [TestMethod]
    public async Task US015S06_RedatorNaoConsegueSalvarNemPorPostDireto()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);
        await SaveAsync(admin, "(11) 91234-5678");
        using HttpClient writer = await SignedInAsync(factory, WriterEmail);

        // O token antiforgery vem de uma página qualquer do painel, já que a do Redator redireciona
        HttpResponseMessage response = await writer.PostFormAsync("/painel/anuncios", Page, new Dictionary<string, string> { ["Phone"] = "(21) 98765-4321" });

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.StartsWith(response.Destination(), "/painel/acesso-negado");
        Assert.AreEqual("11912345678", await PhoneInUseAsync(factory), "o número não mudou");
    }

    [TestMethod]
    public async Task SemTelefoneConfigurado_ATelaAvisa_EDepoisDeSalvarOAvisoSome()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);

        string empty = await (await admin.GetAsync(Page)).TextAsync();
        StringAssert.Contains(empty, "ainda não foi configurado");
        Assert.IsFalse(await IsConfiguredAsync(factory));

        await SaveAsync(admin, "(11) 91234-5678");

        string filled = await (await admin.GetAsync(Page)).TextAsync();
        Assert.DoesNotContain("ainda não foi configurado", filled);
        Assert.IsTrue(await IsConfiguredAsync(factory));
    }

    private static async Task<bool> IsConfiguredAsync(WebFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISiteSettings>().IsPhoneConfiguredAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Pagina_TemRotulo_DicaLigadaAoCampo_CampoDeTelefone()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);

        string html = await (await admin.GetAsync(Page)).TextAsync();

        StringAssert.Contains(html, "Telefone/WhatsApp do site");
        StringAssert.Contains(html, "type=\"tel\"");
        StringAssert.Contains(html, "inputmode=\"tel\"");
        StringAssert.Contains(html, "aria-describedby=\"phone-help error-Phone\"");
        StringAssert.Contains(html, "id=\"phone-help\"");
    }

    [TestMethod]
    public async Task SalvarAuditaOAnteriorEONovo_ESoQuandoMuda()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient admin = await SignedInAsync(factory);
        int adminId = (await factory.ListUsersAsync()).Single(u => u.Email == AdminEmail).Id;

        await SaveAsync(admin, "(11) 91234-5678");
        await SaveAsync(admin, "(21) 98765-4321");
        await SaveAsync(admin, "21 98765-4321"); // mesmo número em outro formato: nada muda

        List<AuditEntry> entries = await AuditAsync(factory);
        Assert.HasCount(2, entries, "o terceiro envio não mudou o número, então não audita");
        Assert.IsTrue(entries.All(e => e.Action == "site_settings.change_phone" && e.TargetId == "site.phone" && e.Result == AuditResult.Success && e.ActorId == adminId));
        Assert.IsNull(entries[0].PreviousValue, "a primeira configuração não tinha valor anterior");
        Assert.AreEqual("11912345678", entries[0].NewValue);
        Assert.AreEqual("11912345678", entries[1].PreviousValue);
        Assert.AreEqual("21987654321", entries[1].NewValue);
    }
}

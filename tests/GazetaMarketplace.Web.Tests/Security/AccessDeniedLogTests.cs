using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// RC-16 (SECURITY_REQUIREMENTS §6): "permissão negada" deixa uma linha de <c>Warning</c> no log, em toda negação do painel — a política por papel (tela "Acesso negado") e a recusa dentro
/// de uma tela (anúncio de outro autor). A linha leva o endereço pedido, o usuário e o papel; nunca e-mail, nome nem o conteúdo do anúncio (R-05).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AccessDeniedLogTests
#pragma warning restore CA1515
{
    private static string[] Denials(DraftSite site) => [.. site.Harness.Factory.Logs.Events
        .Where(e => e.Level == LogEventLevel.Warning && CollectorSink.Text(e).Contains("Permissão negada", StringComparison.Ordinal))
        .Select(CollectorSink.AllAsText)];

    private static string Request(string method, string path) => $"Method=\"{method}\" Path=\"{path}\"";

    [TestMethod]
    public async Task RedatorNaTelaDoAdministrador_DeixaUmaLinhaDePermissaoNegada_ComOEnderecoPedido_OUsuarioEOPapel()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int writerId = await site.UserIdAsync(PanelFixture.WriterEmail);

        HttpResponseMessage response = await site.Writer.GetAsync("/painel/usuarios");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, "a política redireciona para a tela de acesso negado");
        HttpResponseMessage denied = await site.Writer.GetAsync(response.Destination());
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        string line = Assert.ContainsSingle(Denials(site), "uma linha só: a tela de acesso negado não registra de novo");
        StringAssert.Contains(line, Request("GET", "/painel/usuarios"), "o endereço que a pessoa pediu, não o da tela de acesso negado");
        StringAssert.Contains(line, writerId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        StringAssert.Contains(line, "Redator");
        Assert.DoesNotContain(PanelFixture.WriterEmail, line);
    }

    [TestMethod]
    public async Task RedatorAbreAnuncioDeOutro_DeixaUmaLinhaDePermissaoNegada_SemOConteudoDoAnuncio()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await site.AddAdAsync(PanelFixture.AdminEmail, title: "Segredo do outro");

        HttpResponseMessage response = await site.Writer.GetAsync($"/painel/anuncios/{id}/editar");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        string line = Assert.ContainsSingle(Denials(site));
        StringAssert.Contains(line, Request("GET", $"/painel/anuncios/{id}/editar"));
        StringAssert.Contains(line, "Redator");
        Assert.DoesNotContain("Segredo do outro", line);
        Assert.DoesNotContain(PanelFixture.WriterEmail, line);
    }

    [TestMethod]
    public async Task RedatorTentaGravarAnuncioDeOutro_DeixaUmaLinhaDePermissaoNegada_ComOMetodoPost()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await site.AddAdAsync(PanelFixture.AdminEmail, title: "Segredo do outro");

        HttpResponseMessage response = await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", $"/painel/anuncios/{id}/editar", ("Title", "Invadido"));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        StringAssert.Contains(Assert.ContainsSingle(Denials(site)), Request("POST", $"/painel/anuncios/{id}/editar"));
    }

    [TestMethod]
    public async Task AcessoPermitido_NaoDeixaLinhaDePermissaoNegada()
    {
        using DraftSite site = await DraftSite.StartAsync();

        HttpResponseMessage allowed = await site.Admin.GetAsync("/painel/usuarios");
        HttpResponseMessage own = await site.Writer.GetAsync("/painel/anuncios/novo");

        Assert.AreEqual(HttpStatusCode.OK, allowed.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, own.StatusCode);
        Assert.IsEmpty(Denials(site));
    }
}

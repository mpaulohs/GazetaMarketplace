using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Navigation;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PanelMenuTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Redator_VeSoMeusAnuncios()
    {
        IReadOnlyList<MenuItem> items = PanelMenu.Items(Person(RoleNames.Writer), new PathString("/painel/anuncios"));

        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("Meus anúncios", items[0].Text);
        Assert.AreEqual(PanelRoutes.Ads, items[0].Path);
        Assert.IsTrue(items[0].Active);
    }

    [TestMethod]
    public void Administrador_VeOsQuatroItensNaOrdemDoWireframe()
    {
        IReadOnlyList<MenuItem> items = PanelMenu.Items(Person(RoleNames.Administrator), new PathString("/painel/usuarios"));

        CollectionAssert.AreEqual(
            new[] { "Anúncios", "Categorias", "Usuários", "Configurações" },
            items.Select(i => i.Text).ToArray());
        CollectionAssert.AreEqual(
            new[] { PanelRoutes.Ads, PanelRoutes.Categories, PanelRoutes.Users, PanelRoutes.Settings },
            items.Select(i => i.Path).ToArray());
    }

    [TestMethod]
    public void ItemAtivo_EOUnicoQueCasaComOCaminho_InclusiveSubcaminhos()
    {
        IReadOnlyList<MenuItem> items = PanelMenu.Items(Person(RoleNames.Administrator), new PathString("/painel/anuncios/12/editar"));

        Assert.AreEqual(1, items.Count(i => i.Active));
        Assert.IsTrue(items.Single(i => i.Active).Path == PanelRoutes.Ads);
    }

    [TestMethod]
    public void CaminhoParecidoMasDiferente_NaoAtivaOItem()
    {
        IReadOnlyList<MenuItem> items = PanelMenu.Items(Person(RoleNames.Administrator), new PathString("/painel/anunciosx"));

        Assert.AreEqual(0, items.Count(i => i.Active));
    }

    [TestMethod]
    public void SemPapel_OuAnonimo_NaoVeNada()
    {
        Assert.AreEqual(0, PanelMenu.Items(new ClaimsPrincipal(new ClaimsIdentity()), new PathString("/painel/anuncios")).Count);
        Assert.AreEqual(0, PanelMenu.Items(Person("Visitante"), new PathString("/painel/anuncios")).Count);
    }

    [TestMethod]
    public async Task Layout_RenderizaOMenuDoPapel_ComNomeESair()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(WebFactory.RoleHeader, RoleNames.Administrator);

        string html = await (await client.GetAsync("/teste/painel")).Content.ReadAsStringAsync();

        StringAssert.Matches(html, new Regex(@"<nav[^>]*aria-label=""Menu do painel"""));
        foreach (string route in new[] { PanelRoutes.Ads, PanelRoutes.Categories, PanelRoutes.Users, PanelRoutes.Settings })
        {
            StringAssert.Contains(html, "href=\"" + route + "\"");
        }

        StringAssert.Contains(html, "Ana Souza");
        StringAssert.Matches(html, new Regex(@"<button[^>]*>(?:\s*<i[^>]*></i>)?\s*Sair\s*</button>"));
        StringAssert.Matches(html, new Regex(@"<button[^>]*data-bs-toggle=""collapse""[^>]*>[\s\S]*?Menu"));
    }

    [TestMethod]
    public async Task Layout_SemLogin_NaoMostraMenuNemSair()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        string html = await (await client.GetAsync("/teste/painel")).Content.ReadAsStringAsync();

        Assert.IsFalse(html.Contains(PanelRoutes.Ads, System.StringComparison.Ordinal));
        Assert.IsFalse(Regex.IsMatch(html, @"<button[^>]*>(?:\s*<i[^>]*></i>)?\s*Sair\s*</button>"));
    }

    private static ClaimsPrincipal Person(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Ana Souza"), new Claim(ClaimTypes.Role, role)], "Teste"));
}

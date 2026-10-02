using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Web.Navegacao;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class MenuPainelTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Redator_VeSoMeusAnuncios()
    {
        IReadOnlyList<ItemDeMenu> itens = MenuDoPainel.Itens(Pessoa(Papeis.Redator), new PathString("/painel/anuncios"));

        Assert.AreEqual(1, itens.Count);
        Assert.AreEqual("Meus anúncios", itens[0].Texto);
        Assert.AreEqual(RotasDoPainel.Anuncios, itens[0].Caminho);
        Assert.IsTrue(itens[0].Ativo);
    }

    [TestMethod]
    public void Administrador_VeOsQuatroItensNaOrdemDoWireframe()
    {
        IReadOnlyList<ItemDeMenu> itens = MenuDoPainel.Itens(Pessoa(Papeis.Administrador), new PathString("/painel/usuarios"));

        CollectionAssert.AreEqual(
            new[] { "Anúncios", "Categorias", "Usuários", "Configurações" },
            itens.Select(i => i.Texto).ToArray());
        CollectionAssert.AreEqual(
            new[] { RotasDoPainel.Anuncios, RotasDoPainel.Categorias, RotasDoPainel.Usuarios, RotasDoPainel.Configuracoes },
            itens.Select(i => i.Caminho).ToArray());
    }

    [TestMethod]
    public void ItemAtivo_EOUnicoQueCasaComOCaminho_InclusiveSubcaminhos()
    {
        IReadOnlyList<ItemDeMenu> itens = MenuDoPainel.Itens(Pessoa(Papeis.Administrador), new PathString("/painel/anuncios/12/editar"));

        Assert.AreEqual(1, itens.Count(i => i.Ativo));
        Assert.IsTrue(itens.Single(i => i.Ativo).Caminho == RotasDoPainel.Anuncios);
    }

    [TestMethod]
    public void CaminhoParecidoMasDiferente_NaoAtivaOItem()
    {
        IReadOnlyList<ItemDeMenu> itens = MenuDoPainel.Itens(Pessoa(Papeis.Administrador), new PathString("/painel/anunciosx"));

        Assert.AreEqual(0, itens.Count(i => i.Ativo));
    }

    [TestMethod]
    public void SemPapel_OuAnonimo_NaoVeNada()
    {
        Assert.AreEqual(0, MenuDoPainel.Itens(new ClaimsPrincipal(new ClaimsIdentity()), new PathString("/painel/anuncios")).Count);
        Assert.AreEqual(0, MenuDoPainel.Itens(Pessoa("Visitante"), new PathString("/painel/anuncios")).Count);
    }

    [TestMethod]
    public async Task Layout_RenderizaOMenuDoPapel_ComNomeESair()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(FabricaWeb.CabecalhoPapel, Papeis.Administrador);

        string html = await (await cliente.GetAsync("/teste/painel")).Content.ReadAsStringAsync();

        StringAssert.Matches(html, new Regex(@"<nav[^>]*aria-label=""Menu do painel"""));
        foreach (string rota in new[] { RotasDoPainel.Anuncios, RotasDoPainel.Categorias, RotasDoPainel.Usuarios, RotasDoPainel.Configuracoes })
        {
            StringAssert.Contains(html, "href=\"" + rota + "\"");
        }

        StringAssert.Contains(html, "Ana Souza");
        StringAssert.Matches(html, new Regex(@"<button[^>]*>\s*Sair\s*</button>"));
        StringAssert.Matches(html, new Regex(@"<button[^>]*data-bs-toggle=""collapse""[^>]*>[\s\S]*?Menu"));
    }

    [TestMethod]
    public async Task Layout_SemLogin_NaoMostraMenuNemSair()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        string html = await (await cliente.GetAsync("/teste/painel")).Content.ReadAsStringAsync();

        Assert.IsFalse(html.Contains(RotasDoPainel.Anuncios, System.StringComparison.Ordinal));
        Assert.IsFalse(Regex.IsMatch(html, @"<button[^>]*>\s*Sair\s*</button>"));
    }

    private static ClaimsPrincipal Pessoa(string papel) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Ana Souza"), new Claim(ClaimTypes.Role, papel)], "Teste"));
}

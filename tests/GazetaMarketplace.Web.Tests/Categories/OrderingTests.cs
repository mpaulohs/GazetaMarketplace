using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>US-013-S04: mover por botões, só entre irmãs, com a nova ordem valendo no site na hora.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class OrderingTests
#pragma warning restore CA1515
{
    private const int Imoveis = 1;
    private const int Automoveis = 2;
    private const int Autopecas = 3;
    private const int Carros = 33;
    private const int Motos = 36;

    private static async Task<int[]> OrderOfAsync(PanelFixture panel, int? parentId) =>
        [.. (await panel.TreeAsync()).ChildrenOf(parentId).Select(n => n.Id)];

    [TestMethod]
    public async Task US013S04_MoverParaCima_TrocaAOrdem_ENaListaENoSite() // @US-013-S04
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        string before = await panel.IndexAsync();
        Assert.IsTrue(before.IndexOf(">Imóveis<", System.StringComparison.Ordinal) < before.IndexOf(">Automóveis, Peças e Acessórios<", System.StringComparison.Ordinal));

        HttpResponseMessage response = await panel.MoveAsync(Automoveis, "cima");

        string html = await panel.FollowAsync(response);
        Assert.IsTrue(html.IndexOf(">Automóveis, Peças e Acessórios<", System.StringComparison.Ordinal) < html.IndexOf(">Imóveis<", System.StringComparison.Ordinal));
        StringAssert.Contains(html, "Automóveis, Peças e Acessórios agora está antes de Imóveis");
        CollectionAssert.AreEqual(new[] { Automoveis, Imoveis }, (await OrderOfAsync(panel, null)).Take(2).ToArray(), "o site lê a árvore em cache, já na nova ordem");
        StringAssert.EndsWith(response.Destination(), $"#categoria-{Automoveis}", "o redirecionamento leva ao item movido");
    }

    [TestMethod]
    public async Task MoverParaBaixo_EVoltar_RestauraAOrdemOriginal()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int[] original = await OrderOfAsync(panel, Automoveis);

        string down = await panel.FollowAsync(await panel.MoveAsync(Carros, "baixo"));
        int[] moved = await OrderOfAsync(panel, Automoveis);
        await panel.MoveAsync(Carros, "cima");

        StringAssert.Contains(down, "Carros, vans e utilitários agora está depois de Motos");
        Assert.AreEqual(Motos, moved[0], "Motos passou à frente de Carros");
        CollectionAssert.AreEqual(original, await OrderOfAsync(panel, Automoveis));
    }

    [TestMethod]
    public async Task Mover_SoTrocaEntreIrmas_ENuncaMexeEmOutroGrupo()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int[] rootsBefore = await OrderOfAsync(panel, null);
        int[] underAutopecasBefore = await OrderOfAsync(panel, Autopecas);
        int[] underImoveisBefore = await OrderOfAsync(panel, Imoveis);

        await panel.MoveAsync(Carros, "baixo");

        CollectionAssert.AreEqual(rootsBefore, await OrderOfAsync(panel, null));
        CollectionAssert.AreEqual(underAutopecasBefore, await OrderOfAsync(panel, Autopecas));
        CollectionAssert.AreEqual(underImoveisBefore, await OrderOfAsync(panel, Imoveis));
        // A categoria 3 (Autopeças) é irmã de Carros, Motos…: ela entra na troca como qualquer outra, e o terceiro nível dela fica onde está
        Assert.IsTrue((await panel.TreeAsync()).ChildrenOf(Autopecas).All(c => c.ParentId == Autopecas));
    }

    [TestMethod]
    public async Task PrimeiroNaoSobe_UltimoNaoDesce_ENadaMudaNemSeAudita()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int[] roots = await OrderOfAsync(panel, null);

        HttpResponseMessage up = await panel.MoveAsync(roots[0], "cima");
        HttpResponseMessage down = await panel.MoveAsync(roots[^1], "baixo");

        Assert.AreEqual(HttpStatusCode.Redirect, up.StatusCode);
        Assert.AreEqual(HttpStatusCode.Redirect, down.StatusCode);
        CollectionAssert.AreEqual(roots, await OrderOfAsync(panel, null));
        Assert.IsEmpty(await panel.AuditAsync());
        Assert.DoesNotContain("agora está", await panel.FollowAsync(up), "sem aviso quando nada se moveu");
    }

    [TestMethod]
    public async Task Empates_NaOrdem_ViramPosicoesDistintasAntesDaTroca()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        // A carga inicial pode ter irmãs com a mesma ordem: aqui as três primeiras de Imóveis empatam
        await panel.WithDbAsync(async db =>
        {
            await db.Categories.Where(c => c.ParentId == Imoveis && (c.Id == 26 || c.Id == 27 || c.Id == 28)).ExecuteUpdateAsync(s => s.SetProperty(c => c.DisplayOrder, 1));
            return 0;
        });
        panel.Factory.Services.GetRequiredService<ICategoryTree>().Invalidate(); // ExecuteUpdate não passa pelo SaveChanges
        int[] before = await OrderOfAsync(panel, Imoveis);

        await panel.MoveAsync(before[2], "cima");

        int[] after = await OrderOfAsync(panel, Imoveis);
        Assert.AreEqual(before[2], after[1], "o movido subiu uma posição");
        Assert.AreEqual(before[1], after[2], "e a vizinha desceu");
        CategoryNode[] siblings = [.. (await panel.TreeAsync()).ChildrenOf(Imoveis)];
        Assert.AreEqual(siblings.Length, siblings.Select(s => s.DisplayOrder).Distinct().Count(), "nenhum empate sobra");
    }

    [TestMethod]
    public async Task MoverDuasVezesParaCima_VaiDuasPosicoes()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int[] original = await OrderOfAsync(panel, null);

        await panel.MoveAsync(original[4], "cima");
        await panel.MoveAsync(original[4], "cima");

        int[] moved = await OrderOfAsync(panel, null);
        Assert.AreEqual(original[4], moved[2]);
        Assert.AreEqual(original.Length, moved.Length);
        CollectionAssert.AreEquivalent(original, moved);
    }

    [TestMethod]
    public async Task CategoriaNovaEntraNoFimDasIrmas_ESubeComUmClique()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        await panel.CreateAsync("Quadriciclos", Automoveis);
        int id = (await panel.FindByNameAsync("Quadriciclos")).Id;
        int[] siblings = await OrderOfAsync(panel, Automoveis);
        Assert.AreEqual(id, siblings[^1]);

        await panel.MoveAsync(id, "cima");

        Assert.AreEqual(id, (await OrderOfAsync(panel, Automoveis))[^2]);
    }
}

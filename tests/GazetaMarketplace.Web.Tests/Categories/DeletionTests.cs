using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>US-013-S05 e S08 a S10: excluir só o que está vazio, e nunca as categorias da carga que definem o próprio grupo de campos (A7 b).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DeletionTests
#pragma warning restore CA1515
{
    private const int Imoveis = 1;
    private const int Autopecas = 3;
    private const int AluguelDeQuartos = 28;
    private const int Terrenos = 30;
    private const int PecasParaCarros = 38;

    private static async Task<bool> ExistsAsync(PanelFixture panel, int id) => await panel.FindAsync(id) is not null;

    [TestMethod]
    public async Task US013S05_ExcluirUmaCategoriaVazia() // @US-013-S05
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        await panel.CreateAsync("Colecionáveis", null);
        int id = (await panel.FindByNameAsync("Colecionáveis")).Id;
        int before = await panel.CountAsync();

        string confirm = await (await panel.Admin.GetAsync($"{PanelFixture.Page}/{id}/excluir")).TextAsync();
        string html = await panel.FollowAsync(await panel.DeleteAsync(id));

        StringAssert.Contains(confirm, "Excluir Colecionáveis?");
        StringAssert.Contains(html, "Categoria Colecionáveis excluída.");
        Assert.DoesNotContain(">Colecionáveis<", html.Replace("Categoria Colecionáveis excluída.", string.Empty));
        Assert.AreEqual(before - 1, await panel.CountAsync());
        Assert.IsNull((await panel.TreeAsync()).Find(id), "o site deixa de mostrar a categoria na hora");
    }

    [TestMethod]
    public async Task US013S08_ComAnuncios_Recusa_ComAContagemNaMensagem() // @US-013-S08
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        panel.Usage.Counts[AluguelDeQuartos] = 3;

        string html = await panel.FollowAsync(await panel.DeleteAsync(AluguelDeQuartos));

        StringAssert.Contains(html, "Não é possível excluir: 3 anúncios usam esta categoria");
        Assert.IsTrue(await ExistsAsync(panel, AluguelDeQuartos), "a categoria continua na lista");
        StringAssert.Contains(html, ">Aluguel de quartos<");

        panel.Usage.Counts[AluguelDeQuartos] = 1;
        StringAssert.Contains(await panel.FollowAsync(await panel.DeleteAsync(AluguelDeQuartos)), "Não é possível excluir: 1 anúncio usa esta categoria");
    }

    [TestMethod]
    public async Task US013S08_AnuncioEmQualquerSituacao_ContaParaOBloqueio_NoMesmoNumeroDaColuna()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        panel.Usage.Counts[58] = 5; // Ciclismo

        string index = await panel.IndexAsync();
        string html = await panel.FollowAsync(await panel.DeleteAsync(58));

        StringAssert.Contains(index, "5 anúncios");
        StringAssert.Contains(html, "Não é possível excluir: 5 anúncios usam esta categoria");
    }

    [TestMethod]
    public async Task US013S09_ComSubcategorias_Recusa() // @US-013-S09
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int before = await panel.CountAsync();

        string html = await panel.FollowAsync(await panel.DeleteAsync(Imoveis));

        StringAssert.Contains(html, "Exclua ou mova antes as subcategorias desta categoria");
        Assert.AreEqual(before, await panel.CountAsync());
        Assert.IsTrue(await ExistsAsync(panel, Imoveis));
    }

    [TestMethod]
    public async Task US013S10_CategoriaComCamposEspecificos_BloqueiaJaAoClicarEmExcluir_SemPassoDeConfirmacao() // @US-013-S10
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        HttpResponseMessage click = await panel.Admin.GetAsync($"{PanelFixture.Page}/{Terrenos}/excluir");
        string html = await panel.FollowAsync(click);
        HttpResponseMessage forgedPost = await panel.DeleteAsync(Terrenos);

        Assert.AreEqual(HttpStatusCode.Redirect, click.StatusCode, "nenhuma página de confirmação: vai direto à lista com a mensagem");
        StringAssert.Contains(html, "Esta categoria é usada pelos campos específicos e não pode ser excluída. Você pode renomeá-la.");
        StringAssert.Contains(await panel.FollowAsync(forgedPost), "não pode ser excluída");
        Assert.IsTrue(await ExistsAsync(panel, Terrenos));

        // Renomear continua permitido
        Assert.AreEqual(HttpStatusCode.Redirect, (await panel.RenameAsync(Terrenos, "Terrenos e sítios")).StatusCode);
        Assert.AreEqual("Terrenos e sítios", (await panel.FindAsync(Terrenos)).Name);
    }

    [TestMethod]
    public async Task OrdemDosBloqueios_CamposEspecificos_DepoisSubcategorias_DepoisAnuncios()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        panel.Usage.Counts[Autopecas] = 4;
        panel.Usage.Counts[Imoveis] = 4;
        panel.Usage.Counts[Terrenos] = 4;

        // Autopeças: grupo próprio, subcategorias e anúncios ao mesmo tempo -> vale o mais duro
        StringAssert.Contains(await panel.FollowAsync(await panel.DeleteAsync(Autopecas)), "campos específicos");
        // Terrenos: grupo próprio e anúncios -> campos específicos
        StringAssert.Contains(await panel.FollowAsync(await panel.DeleteAsync(Terrenos)), "campos específicos");
        // Imóveis: subcategorias e anúncios -> subcategorias
        StringAssert.Contains(await panel.FollowAsync(await panel.DeleteAsync(Imoveis)), "Exclua ou mova antes as subcategorias");
    }

    [TestMethod]
    public async Task BloqueioPorCamposEspecificos_SoParaCategoriasDaCargaComGrupoProprio()
    {
        HashSet<int> expected = [3, 26, 27, 30, 31, 33, 34, 35, 36, 37, 66, 96];
        CategoryRow[] rows = [.. InitialCategories.All.Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem))];
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build(rows);

        HashSet<int> protectedIds = [.. tree.All.Where(CategoryRules.IsProtectedFromDeletion).Select(n => n.Id)];

        CollectionAssert.AreEquivalent(expected.ToArray(), protectedIds.ToArray());
        Assert.IsFalse(CategoryRules.IsProtectedFromDeletion(tree.Find(PecasParaCarros)), "as peças só herdam o grupo de Autopeças");
        Assert.IsFalse(CategoryRules.IsProtectedFromDeletion(tree.Find(58)), "da carga, mas sem grupo próprio");

        // Criada pelo Administrador, mesmo com um grupo gravado, não é protegida; da carga sem grupo também não
        CategoryNode custom = new(900, 2, "Criada", "criada", 1, true, "Cars", false, 2);
        Assert.IsFalse(CategoryRules.IsProtectedFromDeletion(custom));
        Assert.IsTrue(CategoryRules.IsProtectedFromDeletion(custom with { IsSystem = true }));
    }

    [TestMethod]
    public async Task CategoriaDaCargaSemGrupoProprio_ExcluiNormalmente_ECategoriaDeSistemaHerdeira()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        // Peças para carros: IsSystem, mas só herda o grupo de Autopeças; vazia e sem anúncios, então sai
        string html = await panel.FollowAsync(await panel.DeleteAsync(PecasParaCarros));

        StringAssert.Contains(html, "Categoria Peças para carros, vans e utilitários excluída.");
        Assert.IsFalse(await ExistsAsync(panel, PecasParaCarros));
        Assert.AreEqual(4, (await panel.TreeAsync()).ChildrenOf(Autopecas).Count);
    }

    [TestMethod]
    public async Task ExcluirAUltimaFilha_NaoDevolveAoPaiOStatusDePostavel()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        await panel.CreateAsync("Scooters", 58); // Ciclismo era folha; ganha uma filha e vira grupo
        int id = (await panel.FindByNameAsync("Scooters")).Id;
        Assert.IsFalse((await panel.FindAsync(58)).IsPostable);

        await panel.DeleteAsync(id);

        Assert.IsFalse((await panel.FindAsync(58)).IsPostable, "a mudança de papel foi uma decisão auditada; a exclusão da filha não a desfaz sozinha");
    }

    [TestMethod]
    public async Task PaginaDeConfirmacao_TemCancelarExcluir_ENome()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        string html = await (await panel.Admin.GetAsync($"{PanelFixture.Page}/{AluguelDeQuartos}/excluir")).TextAsync();

        StringAssert.Contains(html, "Excluir Aluguel de quartos?");
        StringAssert.Contains(html, "Essa ação não pode ser desfeita");
        StringAssert.Contains(html, "Cancelar");
        StringAssert.Contains(html, $"action=\"{PanelFixture.Page}/{AluguelDeQuartos}/excluir\"");
    }
}

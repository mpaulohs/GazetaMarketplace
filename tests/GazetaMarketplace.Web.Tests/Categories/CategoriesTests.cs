using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>US-013: o Administrador cria, renomeia, ordena e exclui categorias (S01 a S11), com auditoria (RC-16) e a árvore em cache sempre em dia.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CategoriesTests
#pragma warning restore CA1515
{
    private const int Imoveis = 1;
    private const int Automoveis = 2;
    private const int Autopecas = 3;
    private const int Carros = 33;
    private const int Motos = 36;
    private const int Terrenos = 30;
    private const int PecasParaCarros = 38;

    [TestMethod]
    public async Task US013S01_CriarUmaSubcategoria() // @US-013-S01
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        HttpResponseMessage response = await panel.CreateAsync("Quadriciclos", Automoveis);

        string html = await panel.FollowAsync(response);
        StringAssert.Contains(html, "Categoria Quadriciclos criada.");
        Category created = await panel.FindByNameAsync("Quadriciclos");
        Assert.AreEqual(Automoveis, created.ParentId);
        Assert.AreEqual("quadriciclos", created.Slug);
        Assert.IsTrue(created.IsPostable, "nasce folha: aceita anúncio");
        Assert.IsFalse(created.IsSystem);
        Assert.IsNull(created.FieldGroup, "o grupo é herdado do ancestral (A7 a), não copiado");
        Assert.AreEqual(16, created.DisplayOrder, "vai para o fim das irmãs (a última de Automóveis tem ordem 6)");
        // Aparece abaixo de Automóveis, Peças e Acessórios (recuada, antes da categoria principal seguinte) e o visitante a recebe na árvore
        Assert.IsTrue(html.IndexOf(">Quadriciclos<", System.StringComparison.Ordinal) > html.IndexOf(">Automóveis, Peças e Acessórios<", System.StringComparison.Ordinal));
        Assert.IsTrue(html.IndexOf(">Quadriciclos<", System.StringComparison.Ordinal) < html.IndexOf(">Celulares e Telefonia<", System.StringComparison.Ordinal));
        CategoryTreeSnapshot tree = await panel.TreeAsync();
        Assert.IsTrue(tree.ChildrenOf(Automoveis).Any(c => c.Name == "Quadriciclos"), "a árvore em cache já mostra a nova categoria");
    }

    [TestMethod]
    public async Task US013S02_CriarUmaCategoriaPrincipal() // @US-013-S02
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        string html = await panel.FollowAsync(await panel.CreateAsync("Colecionáveis", null));

        StringAssert.Contains(html, "Categoria Colecionáveis criada.");
        Category created = await panel.FindByNameAsync("Colecionáveis");
        Assert.IsNull(created.ParentId);
        CategoryTreeSnapshot tree = await panel.TreeAsync();
        Assert.AreEqual("Colecionáveis", tree.Roots[^1].Name, "a página inicial lê as principais da árvore: a nova é a última");
        Assert.AreEqual(1, tree.Find(created.Id).Depth);
    }

    [TestMethod]
    public async Task US013S03_RenomearUmaCategoria_MantemOId_OSlug_EOGrupoDeCampos() // @US-013-S03
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        Category before = await panel.FindAsync(Carros);

        string html = await panel.FollowAsync(await panel.RenameAsync(Carros, "Veículos de passeio"));

        StringAssert.Contains(html, "Categoria renomeada para Veículos de passeio.");
        Assert.DoesNotContain(">Carros, vans e utilitários<", html);
        StringAssert.Contains(html, ">Veículos de passeio<");
        Category after = await panel.FindAsync(Carros);
        Assert.AreEqual("Veículos de passeio", after.Name);
        Assert.AreEqual(before.Slug, after.Slug, "o endereço público não muda ao renomear");
        Assert.AreEqual(before.FieldGroup, after.FieldGroup);
        Assert.AreEqual(before.ParentId, after.ParentId);
        Assert.AreEqual("Veículos de passeio", (await panel.TreeAsync()).Find(Carros).Name);
    }

    [TestMethod]
    public async Task US013S06_NomeRepetido_EmQualquerCaixaOuAcento_ENoMesmoGrupo() // @US-013-S06
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int before = await panel.CountAsync();

        foreach (string name in new[] { "Motos", "motos", "  MOTOS  ", "Mótos" })
        {
            HttpResponseMessage response = await panel.CreateAsync(name, Automoveis);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, name);
            StringAssert.Contains(await response.TextAsync(), "Já existe uma categoria com esse nome neste grupo");
        }

        Assert.AreEqual(before, await panel.CountAsync(), "nenhuma foi criada");
        Assert.AreEqual(HttpStatusCode.Redirect, (await panel.CreateAsync("Motos", Imoveis)).StatusCode, "o mesmo nome em outro grupo é permitido");
    }

    [TestMethod]
    public async Task US013S06_RenomearParaONomeDeUmaIrma_Recusa_MasTrocarSoACaixaPode()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        HttpResponseMessage duplicate = await panel.RenameAsync(Carros, "Motos");
        HttpResponseMessage caseOnly = await panel.RenameAsync(Carros, "CARROS, VANS E UTILITÁRIOS");

        Assert.AreEqual(HttpStatusCode.OK, duplicate.StatusCode);
        StringAssert.Contains(await duplicate.TextAsync(), "Já existe uma categoria com esse nome neste grupo");
        Assert.AreEqual(HttpStatusCode.Redirect, caseOnly.StatusCode, "a própria categoria não conta como repetida");
        Assert.AreEqual("CARROS, VANS E UTILITÁRIOS", (await panel.FindAsync(Carros)).Name);
    }

    [TestMethod]
    public async Task US013S07_NomeVazio_OuMuitoLongo() // @US-013-S07
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int before = await panel.CountAsync();

        foreach (string empty in new[] { "", "   " })
        {
            HttpResponseMessage response = await panel.CreateAsync(empty, Automoveis);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            StringAssert.Contains(await response.TextAsync(), "Informe o nome da categoria");
        }

        HttpResponseMessage tooLong = await panel.CreateAsync(new string('a', 101), Automoveis);
        StringAssert.Contains(await tooLong.TextAsync(), "no máximo 100 caracteres");
        StringAssert.Contains(await (await panel.RenameAsync(Carros, " ")).TextAsync(), "Informe o nome da categoria");
        Assert.AreEqual(before, await panel.CountAsync());
        Assert.AreEqual("Carros, vans e utilitários", (await panel.FindAsync(Carros)).Name);
    }

    [TestMethod]
    public async Task US013S11_AteTresNiveis_AListaDePaiNaoTemOTerceiro_EOServidorRecusaOQuartoForjado() // @US-013-S11
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        string created = await panel.FollowAsync(await panel.CreateAsync("Peças para quadriciclos", Autopecas));
        Category third = await panel.FindByNameAsync("Peças para quadriciclos");
        Assert.AreEqual(3, (await panel.TreeAsync()).Find(third.Id).Depth);
        StringAssert.Contains(created, "Categoria Peças para quadriciclos criada.");

        string form = await (await panel.Admin.GetAsync($"{PanelFixture.Page}/nova")).TextAsync();
        StringAssert.Contains(form, ">— Nenhuma (categoria principal) —<");
        StringAssert.Contains(form, $"value=\"{Autopecas}\"", "Autopeças (segundo nível) é opção");
        StringAssert.Contains(form, $"value=\"{Automoveis}\"");
        Assert.DoesNotContain($"value=\"{PecasParaCarros}\"", form, "Peças para carros (terceiro nível) não é opção");
        Assert.DoesNotContain($"value=\"{third.Id}\"", form);

        // O servidor não confia na lista: um POST forjado com pai de terceiro nível é recusado
        int before = await panel.CountAsync();
        HttpResponseMessage forged = await panel.CreateAsync("Quarto nível", PecasParaCarros);
        Assert.AreEqual(HttpStatusCode.OK, forged.StatusCode);
        StringAssert.Contains(await forged.TextAsync(), "As categorias vão até o terceiro nível");
        Assert.AreEqual(before, await panel.CountAsync());
    }

    [TestMethod]
    public async Task PaiInexistente_Recusa()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        HttpResponseMessage response = await panel.CreateAsync("Órfã", 99999);

        StringAssert.Contains(await response.TextAsync(), "Escolha uma categoria pai válida");
        Assert.IsNull(await panel.FindByNameAsync("Órfã"));
    }

    [TestMethod]
    public async Task SubcategoriaDentroDeUmaFolhaSemAnuncios_TiraDaFolhaOStatusDePostavel_ESegueAuditado()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        Assert.IsTrue((await panel.FindAsync(Motos)).IsPostable);

        Assert.AreEqual(HttpStatusCode.Redirect, (await panel.CreateAsync("Scooters", Motos)).StatusCode);

        Assert.IsFalse((await panel.FindAsync(Motos)).IsPostable, "Motos deixou de ser folha: não recebe mais anúncio");
        Assert.IsTrue((await panel.FindByNameAsync("Scooters")).IsPostable);
        Assert.IsFalse((await panel.TreeAsync()).Find(Motos).IsPostable);
        AuditEntry change = (await panel.AuditAsync()).Single(e => e.Action == "category.change_postable");
        Assert.AreEqual(Motos.ToString(System.Globalization.CultureInfo.InvariantCulture), change.TargetId);
        Assert.AreEqual("aceita anúncios", change.PreviousValue);
        Assert.AreEqual("grupo (só subcategorias)", change.NewValue);
    }

    [TestMethod]
    public async Task SubcategoriaDentroDeUmaFolhaComAnuncios_Recusa_ENadaMuda()
    {
        using PanelFixture panel = await PanelFixture.StartAsync(realAds: true);
        await panel.AddAdAsync(Motos);
        await panel.AddAdAsync(Motos, AdStatus.Published);
        int before = await panel.CountAsync();

        HttpResponseMessage response = await panel.CreateAsync("Scooters", Motos);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.TextAsync(), "Mova antes os anúncios desta categoria");
        Assert.AreEqual(before, await panel.CountAsync());
        Assert.IsTrue((await panel.FindAsync(Motos)).IsPostable);
        Assert.IsEmpty(await panel.AuditAsync(), "recusar não deixa rastro");
    }

    [TestMethod]
    public async Task SlugDaCategoriaNova_E_Unico_ComSufixo()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        await panel.CreateAsync("Motos", Imoveis); // o slug "motos" já é da categoria 36
        await panel.CreateAsync("Motos", 4);

        Assert.AreEqual("motos-2", (await panel.WithDbAsync(async db => await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Categories, c => c.ParentId == Imoveis && c.Name == "Motos"))).Slug);
        Assert.AreEqual("motos-3", (await panel.WithDbAsync(async db => await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Categories, c => c.ParentId == 4 && c.Name == "Motos"))).Slug);
    }

    [TestMethod]
    public async Task CriarRenomearReordenarExcluir_GravamAuditoria_ComAnteriorENovo()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int adminId = (await panel.Factory.ListUsersAsync()).Single(u => u.Email == PanelFixture.AdminEmail).Id;

        int roots = (await panel.TreeAsync()).Roots.Count;
        await panel.CreateAsync("Colecionáveis", null);
        int id = (await panel.FindByNameAsync("Colecionáveis")).Id;
        await panel.RenameAsync(id, "Coleções");
        await panel.MoveAsync(id, "cima");
        await panel.DeleteAsync(id);

        var entries = await panel.AuditAsync();
        CollectionAssert.AreEqual(
            new[] { "category.create", "category.rename", "category.move", "category.delete" }, entries.Select(e => e.Action).ToArray());
        Assert.IsTrue(entries.All(e => e.TargetType == "Category" && e.Result == AuditResult.Success && e.ActorId == adminId));
        Assert.IsTrue(entries.All(e => e.TargetId == id.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.IsNull(entries[0].PreviousValue);
        Assert.AreEqual("Colecionáveis", entries[0].NewValue);
        Assert.AreEqual("Colecionáveis", entries[1].PreviousValue);
        Assert.AreEqual("Coleções", entries[1].NewValue);
        Assert.AreEqual($"posição {roots + 1}", entries[2].PreviousValue, "era a última entre as principais");
        Assert.AreEqual($"posição {roots}", entries[2].NewValue);
        Assert.AreEqual("Coleções", entries[3].PreviousValue);
        Assert.IsNull(entries[3].NewValue);
    }

    [TestMethod]
    public async Task RenomearParaOMesmoNome_NaoGravaNemAudita()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        Assert.AreEqual(HttpStatusCode.Redirect, (await panel.RenameAsync(Carros, " Carros, vans e utilitários ")).StatusCode);

        Assert.IsEmpty(await panel.AuditAsync());
        Assert.IsNull((await panel.FindAsync(Carros)).UpdatedAt);
    }

    [TestMethod]
    public async Task Recusas_NaoGravamAuditoria()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        await panel.CreateAsync("Motos", Automoveis);
        await panel.CreateAsync("", Automoveis);
        await panel.DeleteAsync(Terrenos);
        await panel.DeleteAsync(Imoveis);

        Assert.IsEmpty(await panel.AuditAsync());
    }

    [TestMethod]
    public async Task OutroRedatorOuAnonimo_NaoMexeEmCategorias()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        using HttpClient writer = await PanelFixture.SignedInAsync(panel.Factory, PanelFixture.WriterEmail);
        using HttpClient anonymous = TeamClient.Create(panel.Factory);
        int before = await panel.CountAsync();

        HttpResponseMessage page = await writer.GetAsync(PanelFixture.Page);
        Assert.AreEqual(HttpStatusCode.Redirect, page.StatusCode);
        StringAssert.StartsWith(page.Destination(), "/painel/acesso-negado");
        HttpResponseMessage denied = await writer.GetAsync(page.Destination());
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        StringAssert.Contains(await denied.TextAsync(), "Você não tem permissão para acessar esta página");

        // Nem por POST direto: o Redator tem token válido (vem de outra página do painel) e mesmo assim é barrado
        HttpResponseMessage forged = await writer.PostFormAsync("/painel/anuncios", $"{PanelFixture.Page}/nova", new System.Collections.Generic.Dictionary<string, string> { ["Name"] = "Intrusa" });
        Assert.AreEqual(HttpStatusCode.Redirect, forged.StatusCode);
        StringAssert.StartsWith(forged.Destination(), "/painel/acesso-negado");
        Assert.AreEqual(HttpStatusCode.Redirect, (await anonymous.GetAsync(PanelFixture.Page)).StatusCode);
        StringAssert.StartsWith((await anonymous.GetAsync(PanelFixture.Page)).Destination(), "/painel/entrar");
        Assert.AreEqual(before, await panel.CountAsync());
    }

    [TestMethod]
    public async Task PostSemTokenAntiforgery_ERecusado()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        int before = await panel.CountAsync();
        using FormUrlEncodedContent form = new(new System.Collections.Generic.Dictionary<string, string> { ["Name"] = "Sem token" });

        HttpResponseMessage response = await panel.Admin.PostAsync($"{PanelFixture.Page}/nova", form);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(before, await panel.CountAsync());
    }

    [TestMethod]
    public async Task CategoriaQueNaoExiste_Devolve404_EmTodasAsAcoes()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        Assert.AreEqual(HttpStatusCode.NotFound, (await panel.Admin.GetAsync($"{PanelFixture.Page}/99999/editar")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await panel.Admin.GetAsync($"{PanelFixture.Page}/99999/excluir")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await panel.RenameAsync(99999, "Nome")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await panel.MoveAsync(99999, "cima")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await panel.DeleteAsync(99999)).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await panel.MoveAsync(Imoveis, "lado")).StatusCode, "direção que não existe");
    }

    [TestMethod]
    public async Task Tela_TemListasAninhadas_NomesAcessiveis_EOsBotoesDosExtremosInativos()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();
        panel.Usage.Counts[Carros] = 4;
        panel.Usage.Counts[Motos] = 1;

        string html = await panel.IndexAsync();

        StringAssert.Contains(html, "aria-label=\"Mover Imóveis para baixo\"");
        StringAssert.Contains(html, "aria-label=\"Editar Imóveis\"");
        StringAssert.Contains(html, "aria-label=\"Excluir Imóveis\"");
        StringAssert.Contains(html, "Categoria principal: ");
        StringAssert.Contains(html, "Subcategoria de Automóveis, Peças e Acessórios: ");
        StringAssert.Contains(html, "Subcategoria de Autopeças: ", "o terceiro nível anuncia o pai direto");
        StringAssert.Contains(html, "4 anúncios");
        StringAssert.Contains(html, "1 anúncio<");
        // O primeiro de cada grupo não sobe e o último não desce: botão presente, mas inativo e sem formulário
        StringAssert.Contains(html, $"id=\"mover-{Imoveis}-cima\" aria-disabled=\"true\"");
        StringAssert.Contains(html, "id=\"mover-23-baixo\" aria-disabled=\"true\"");
        Assert.DoesNotContain($"/{Imoveis}/mover/cima", html);
        StringAssert.Contains(html, $"/{Imoveis}/mover/baixo");
        // Listas dentro de listas: o terceiro nível está dentro de Autopeças, dentro de Automóveis
        int depth = 0;
        int maxDepth = 0;
        foreach (System.Text.RegularExpressions.Match tag in System.Text.RegularExpressions.Regex.Matches(html, @"<(/?)ul[ >]"))
        {
            depth += tag.Groups[1].Value == "/" ? -1 : 1;
            maxDepth = System.Math.Max(maxDepth, depth);
            Assert.IsGreaterThanOrEqualTo(0, depth, "lista fechada sem ter sido aberta");
        }

        Assert.AreEqual(0, depth, "todas as listas fechadas");
        Assert.IsGreaterThanOrEqualTo(3, maxDepth, "raiz + subcategorias + terceiro nível");
        StringAssert.Contains(html, "role=\"alertdialog\"");
        StringAssert.Contains(html, "/js/pages/categories-index.js");
    }

    [TestMethod]
    public async Task Tela_SoOferecaAJanelaDeConfirmacaoParaQuemPodeSerExcluida()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        string html = await panel.IndexAsync();

        // Terrenos tem campos específicos: o link marca data-confirm="false", então o JavaScript o deixa seguir direto para a mensagem
        System.Text.RegularExpressions.Match terrenos = System.Text.RegularExpressions.Regex.Match(html, @"<a[^>]*aria-label=""Excluir Terrenos[^>]*>");
        System.Text.RegularExpressions.Match imoveis = System.Text.RegularExpressions.Regex.Match(html, @"<a[^>]*aria-label=""Excluir Imóveis""[^>]*>");
        Assert.IsTrue(terrenos.Success && imoveis.Success);
        StringAssert.Contains(terrenos.Value, "data-confirm=\"false\"");
        Assert.DoesNotContain("data-confirm=\"true\"", terrenos.Value);
        StringAssert.Contains(imoveis.Value, "data-confirm=\"true\"");
    }

    [TestMethod]
    public async Task FormularioDeNovaCategoria_TemRotulos_EAjudaLigadaAosCampos()
    {
        using PanelFixture panel = await PanelFixture.StartAsync();

        string html = await (await panel.Admin.GetAsync($"{PanelFixture.Page}/nova")).TextAsync();

        StringAssert.Contains(html, "Nome");
        StringAssert.Contains(html, "Categoria pai");
        StringAssert.Contains(html, "aria-describedby=\"parent-help error-ParentId\"");
        StringAssert.Contains(html, "id=\"parent-help\"");
        StringAssert.Contains(html, "aria-required=\"true\"");
    }
}

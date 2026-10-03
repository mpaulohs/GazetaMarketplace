using System.Linq;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>A árvore em memória: filhas, descendentes de todos os níveis, ancestrais, caminho e grupo de campos herdado.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class TreeTests
#pragma warning restore CA1515
{
    private static CategoryTreeSnapshot Real() => CategoryTreeSnapshot.Build(
        InitialCategories.All.Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem)));

    private static CategoryRow Row(int id, int? parent, string group = null, int order = 1, string name = null) =>
        new(id, parent, name ?? "C" + id, "c" + id, order, true, group, false);

    [TestMethod]
    public void Descendentes_IncluemTodosOsNiveis()
    {
        CategoryTreeSnapshot tree = Real();

        int[] descendants = [.. tree.DescendantsOf(2).Select(n => n.Id)];

        // 5 veículos + Autopeças + as 5 peças do terceiro nível
        Assert.HasCount(11, descendants);
        CollectionAssert.IsSubsetOf(new[] { 33, 34, 35, 36, 37, 3, 38, 39, 40, 41, 42 }, descendants);
        Assert.DoesNotContain(2, descendants, "a própria categoria não entra");
        Assert.IsEmpty(tree.DescendantsOf(38), "folha não tem descendentes");
        Assert.IsEmpty(tree.DescendantsOf(999));
    }

    [TestMethod]
    public void Descendentes_VemEmOrdemDeExibicao_PaiAntesDasFilhas()
    {
        CategoryTreeSnapshot tree = Real();

        CollectionAssert.AreEqual(new[] { 33, 36, 35, 34, 37, 3, 38, 40, 42, 39, 41 }, tree.DescendantsOf(2).Select(n => n.Id).ToArray());
    }

    [TestMethod]
    public void Raizes_SaoAs22DePrimeiroNivel_EmOrdemDeExibicao()
    {
        CategoryTreeSnapshot tree = Real();

        Assert.HasCount(22, tree.Roots);
        CollectionAssert.AreEqual(tree.Roots.OrderBy(n => n.DisplayOrder).Select(n => n.Id).ToArray(), tree.Roots.Select(n => n.Id).ToArray());
        Assert.AreEqual(1, tree.Roots[0].Id);
        Assert.IsTrue(tree.Roots.All(n => n.Depth == 1 && n.ParentId is null));
    }

    [TestMethod]
    public void FilhasDiretas_SeguemADisplayOrder_NaoOId()
    {
        CategoryTreeSnapshot tree = Real();

        // Em "Automóveis" a ordem não segue o id: Carros 1, Motos 2, Ônibus 3, Caminhões 4, Barcos 5, Autopeças 6
        CollectionAssert.AreEqual(new[] { 33, 36, 35, 34, 37, 3 }, tree.ChildrenOf(2).Select(n => n.Id).ToArray());
    }

    [TestMethod]
    public void Ancestrais_ECaminho_VaoDoPrimeiroNivelParaBaixo()
    {
        CategoryTreeSnapshot tree = Real();

        CollectionAssert.AreEqual(new[] { 2, 3 }, tree.AncestorsOf(38).Select(n => n.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 3, 38 }, tree.PathTo(38).Select(n => n.Id).ToArray());
        Assert.IsEmpty(tree.AncestorsOf(2));
        Assert.IsEmpty(tree.PathTo(12345));
    }

    [TestMethod]
    public void Busca_PorIdEPorSlug()
    {
        CategoryTreeSnapshot tree = Real();

        Assert.AreEqual(33, tree.FindBySlug("cars").Id);
        Assert.AreEqual(66, tree.FindBySlug("servicos").Id);
        Assert.AreEqual(7, tree.FindBySlug("servicos-grupo").Id);
        Assert.IsNull(tree.FindBySlug("nao-existe"));
        Assert.IsNull(tree.FindBySlug(null));
        Assert.IsNull(tree.Find(79), "animal vivo fora da v1");
        Assert.AreEqual(147, tree.Count);
    }

    [TestMethod]
    public void GrupoDeCampos_ProprioVenceOHerdado_ESemNenhumDevolveNulo()
    {
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build(
        [
            Row(1, null, group: "Cars"),
            Row(2, 1),
            Row(3, 2, group: "Parts"),
            Row(4, 3),
            Row(5, null),
            Row(6, 5)
        ]);

        Assert.AreEqual("Cars", tree.ResolveFieldGroup(1));
        Assert.AreEqual("Cars", tree.ResolveFieldGroup(2), "herda do pai");
        Assert.AreEqual("Parts", tree.ResolveFieldGroup(3), "o próprio vence");
        Assert.AreEqual("Parts", tree.ResolveFieldGroup(4), "herda do ancestral mais próximo, não do primeiro");
        Assert.IsNull(tree.ResolveFieldGroup(6));
        Assert.IsNull(tree.ResolveFieldGroup(999));
    }

    [TestMethod]
    public void LinhaComPaiInexistenteOuEmCiclo_FicaForaDaArvore()
    {
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build(
        [
            Row(1, null),
            Row(2, 99),        // pai inexistente
            Row(3, 2),         // filha de quem está fora
            Row(4, 5),
            Row(5, 4)          // ciclo
        ]);

        CollectionAssert.AreEqual(new[] { 1 }, tree.All.Select(n => n.Id).ToArray());
    }

    [TestMethod]
    public void ArvoreVazia_FuncionaSemExcecao()
    {
        Assert.IsEmpty(CategoryTreeSnapshot.Empty.Roots);
        Assert.IsEmpty(CategoryTreeSnapshot.Empty.All);
        Assert.IsNull(CategoryTreeSnapshot.Empty.Find(1));
    }

    [TestMethod]
    public void Profundidade_VemDaPosicaoNaArvore()
    {
        CategoryTreeSnapshot tree = Real();

        Assert.AreEqual(1, tree.Find(2).Depth);
        Assert.AreEqual(2, tree.Find(3).Depth);
        Assert.AreEqual(3, tree.Find(38).Depth);
        Assert.AreEqual(2, tree.Find(33).Depth);
    }
}

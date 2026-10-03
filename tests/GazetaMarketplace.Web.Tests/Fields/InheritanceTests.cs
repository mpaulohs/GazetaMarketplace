using System;
using System.Linq;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>Herança do grupo de campos pela árvore (A7 a): o próprio, o do ancestral mais próximo ou Produtos em geral.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class InheritanceTests
#pragma warning restore CA1515
{
    private static CategoryRow[] Seed() =>
        [.. InitialCategories.All.Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem))];

    private static CategoryTreeSnapshot WithExtra(params CategoryRow[] extra) => CategoryTreeSnapshot.Build([.. Seed(), .. extra]);

    [TestMethod]
    public void CategoriaDaCarga_ComGrupoProprio_UsaEsseGrupo()
    {
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build(Seed());

        Assert.AreEqual(FieldGroupKeys.RealEstate, FieldGroupRegistry.Resolve(tree, 26).Key);
        Assert.AreEqual(FieldGroupKeys.RealEstate, FieldGroupRegistry.Resolve(tree, 30).Key);
        Assert.AreEqual(FieldGroupKeys.Services, FieldGroupRegistry.Resolve(tree, 66).Key);
        Assert.AreEqual(FieldGroupKeys.Jobs, FieldGroupRegistry.Resolve(tree, 96).Key);
    }

    [TestMethod]
    public void CategoriaNova_HerdaOGrupoDoPai()
    {
        CategoryTreeSnapshot tree = WithExtra(
            new CategoryRow(156, 26, "Studios", "studios", 9, true, null, false),
            new CategoryRow(157, 156, "Studios de luxo", "studios-de-luxo", 1, true, null, false));

        Assert.AreEqual(FieldGroupKeys.RealEstate, FieldGroupRegistry.Resolve(tree, 156).Key, "filha de Apartamentos");
        Assert.AreEqual(FieldGroupKeys.RealEstate, FieldGroupRegistry.Resolve(tree, 157).Key, "neta: o ancestral mais próximo com grupo");
    }

    [TestMethod]
    public void GrupoProprioVenceOHerdado()
    {
        CategoryTreeSnapshot tree = WithExtra(new CategoryRow(156, 26, "Vagas de garagem", "vagas-de-garagem", 9, true, FieldGroupKeys.Jobs, false));

        Assert.AreEqual(FieldGroupKeys.Jobs, FieldGroupRegistry.Resolve(tree, 156).Key);
        Assert.AreEqual(FieldGroupKeys.RealEstate, FieldGroupRegistry.Resolve(tree, 27).Key, "a irmã de outra linha não muda");
    }

    [TestMethod]
    public void CategoriaSemGrupoNaCadeia_CaiEmProdutosEmGeral()
    {
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build(Seed());

        // Camas e Colchões (135), Móveis (21) e Papelaria (155) nunca terão grupo próprio: "as demais categorias ativas"
        foreach (int id in new[] { 135, 140, 155, 21 })
        {
            Assert.AreSame(FieldGroupRegistry.Default, FieldGroupRegistry.Resolve(tree, id), $"categoria {id}");
        }
    }

    [TestMethod]
    public void CategoriaInexistente_DevolveNulo()
    {
        Assert.IsNull(FieldGroupRegistry.Resolve(CategoryTreeSnapshot.Build(Seed()), 9999));
        Assert.IsNull(FieldGroupRegistry.Resolve(CategoryTreeSnapshot.Build(Seed()), 79), "animal vivo, fora da v1");
    }

    [TestMethod]
    public void ChaveDeGrupoGravadaMasInexistente_LancaEmVezDeEscolherEmSilencio()
    {
        CategoryTreeSnapshot tree = WithExtra(new CategoryRow(156, 135, "Colchões", "colchoes", 9, true, "GrupoQueNaoExiste", false));

        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(() => FieldGroupRegistry.Resolve(tree, 156));
        StringAssert.Contains(error.Message, "GrupoQueNaoExiste");
    }

    [TestMethod]
    public void TodoGrupoGravadoNaCarga_ExisteNoRegistro()
    {
        string[] stored = [.. InitialCategories.All.Where(c => c.FieldGroup is not null).Select(c => c.FieldGroup).Distinct()];

        Assert.IsNotEmpty(stored);
        foreach (string key in stored)
        {
            Assert.IsNotNull(FieldGroupRegistry.Get(key), $"o grupo '{key}' está gravado em Categories mas não existe no registro");
        }
    }

    [TestMethod]
    public void CamposDaCategoria_VemDoGrupoResolvido()
    {
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build(Seed());

        FieldGroup forTerrenos = FieldGroupRegistry.Resolve(tree, 30)!;

        Assert.IsFalse(forTerrenos.FieldsFor(30).Any(f => f.Key == "bedrooms"));
        Assert.IsTrue(FieldGroupRegistry.Resolve(tree, 26)!.FieldsFor(26).Any(f => f.Key == "bedrooms"));
    }
}

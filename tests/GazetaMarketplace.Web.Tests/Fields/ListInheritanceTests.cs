using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>
/// R-06 e A7 a (SPEC v1.9): a categoria herda do ancestral mais próximo o grupo <b>e</b> os dados por categoria dele: as listas de opções, os campos que existem e os obrigatórios.
/// Antes, uma filha de Imóveis, Roupas, Eletro ou Telefonia ficava com o campo obrigatório de lista sem nenhuma opção e o anúncio nunca podia ser enviado.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ListInheritanceTests
#pragma warning restore CA1515
{
    private static CategoryRow[] Seed() =>
        [.. InitialCategories.All.Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem))];

    // Uma filha e uma neta nova sob cada categoria que define o próprio grupo
    private static (CategoryTreeSnapshot Tree, List<(int Owner, int Child, int Grandchild)> New) TreeWithChildren()
    {
        List<CategoryRow> extra = [];
        List<(int, int, int)> added = [];
        int next = 1000;
        foreach (CategoryRow owner in Seed().Where(c => c.FieldGroup is not null))
        {
            int child = next++;
            int grandchild = next++;
            extra.Add(new CategoryRow(child, owner.Id, "Filha " + owner.Id, "filha-" + owner.Id, 99, true, null, false));
            extra.Add(new CategoryRow(grandchild, child, "Neta " + owner.Id, "neta-" + owner.Id, 1, true, null, false));
            added.Add((owner.Id, child, grandchild));
        }

        return (CategoryTreeSnapshot.Build([.. Seed(), .. extra]), added);
    }

    [TestMethod]
    public void FilhaENeta_TemOsMesmosCamposObrigatoriosEListasDoAncestralQueDefineOGrupo_EmTodosOsGrupos()
    {
        (CategoryTreeSnapshot tree, List<(int Owner, int Child, int Grandchild)> added) = TreeWithChildren();
        Assert.IsGreaterThan(40, added.Count, "uma filha para cada categoria com grupo próprio da carga");

        foreach ((int owner, int child, int grandchild) in added)
        {
            FieldGroup ownerGroup = FieldGroupRegistry.Resolve(tree, owner);
            foreach (int descendant in new[] { child, grandchild })
            {
                FieldGroup group = FieldGroupRegistry.Resolve(tree, descendant);
                Assert.AreEqual(ownerGroup.Key, group.Key, $"categoria {descendant} (sob {owner}): mesmo grupo");
                CollectionAssert.AreEqual(
                    ownerGroup.FieldsFor(owner).Select(f => f.Key).ToArray(), group.FieldsFor(descendant).Select(f => f.Key).ToArray(), $"categoria {descendant} (sob {owner}): os mesmos campos");
                CollectionAssert.AreEqual(
                    ownerGroup.RequiredFieldsFor(owner).Select(f => f.Key).ToArray(), group.RequiredFieldsFor(descendant).Select(f => f.Key).ToArray(), $"categoria {descendant} (sob {owner}): os mesmos obrigatórios");
                foreach (FieldDefinition field in group.FieldsFor(descendant))
                {
                    FieldDefinition ownerField = ownerGroup.Field(field.Key);
                    Assert.AreSame(ownerField.OptionsFor(owner), field.OptionsFor(descendant), $"categoria {descendant} (sob {owner}): a lista de '{field.Key}'");
                    Assert.AreEqual(ownerField.IsRequiredFor(owner), field.IsRequiredFor(descendant), $"categoria {descendant} (sob {owner}): '{field.Key}' obrigatório");
                }

                foreach (FieldDefinition required in group.RequiredFieldsFor(descendant).Where(f => f.Type is FieldType.Select or FieldType.MultiSelect))
                {
                    Assert.IsNotNull(required.OptionsFor(descendant), $"categoria {descendant}: o campo obrigatório de lista '{required.Key}' tem lista");
                    Assert.IsNotEmpty(required.OptionsFor(descendant).Options, $"categoria {descendant}: '{required.Key}' tem opções");
                }
            }
        }
    }

    [TestMethod]
    public void FilhaDeImoveis_UsaAListaDoPai_ENaoADeOutraCategoriaDoGrupo()
    {
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build([.. Seed(),
            new CategoryRow(900, 26, "Flats", "flats", 9, true, null, false), new CategoryRow(901, 30, "Chácaras", "chacaras", 9, true, null, false)]);

        FieldDefinition underApartments = FieldGroupRegistry.Resolve(tree, 900).Field("propertyTypeId");
        FieldDefinition underLand = FieldGroupRegistry.Resolve(tree, 901).Field("propertyTypeId");

        Assert.AreSame(FieldGroupRegistry.Resolve(tree, 26).Field("propertyTypeId").OptionsFor(26), underApartments.OptionsFor(900));
        Assert.AreSame(FieldGroupRegistry.Resolve(tree, 30).Field("propertyTypeId").OptionsFor(30), underLand.OptionsFor(901));
        Assert.AreNotSame(underApartments.OptionsFor(900), underLand.OptionsFor(901), "as duas listas são diferentes: herdar é por ancestral, não uma lista única");
        CollectionAssert.Contains(FieldGroupRegistry.Resolve(tree, 900).FieldsFor(900).Select(f => f.Key).ToArray(), "bedrooms", "quartos existem na filha de Apartamentos");
        CollectionAssert.DoesNotContain(FieldGroupRegistry.Resolve(tree, 901).FieldsFor(901).Select(f => f.Key).ToArray(), "bedrooms", "e não na de Terrenos");
        Assert.IsTrue(FieldGroupRegistry.Resolve(tree, 901).Field("areaM2").IsRequiredFor(901), "a área continua obrigatória na filha de Terrenos");
    }

    [TestMethod]
    public void CategoriaDaCarga_ContinuaComOMesmoGrupoDoRegistro_SemCopia()
    {
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build(Seed());

        foreach (CategoryRow row in Seed().Where(c => c.FieldGroup is not null))
        {
            Assert.AreSame(FieldGroupRegistry.Get(row.FieldGroup), FieldGroupRegistry.Resolve(tree, row.Id), $"categoria {row.Id}");
        }
    }

    [TestMethod]
    public void MesmaFilha_ResolvidaDuasVezes_ReusaOGrupoDerivado()
    {
        CategoryTreeSnapshot tree = CategoryTreeSnapshot.Build([.. Seed(), new CategoryRow(900, 26, "Flats", "flats", 9, true, null, false)]);

        Assert.AreSame(FieldGroupRegistry.Resolve(tree, 900), FieldGroupRegistry.Resolve(tree, 900));
    }
}

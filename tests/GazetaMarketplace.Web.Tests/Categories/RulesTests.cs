using System.Linq;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>Regras puras da categoria nova ou renomeada: profundidade, nome vazio e nome repetido entre irmãs.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class RulesTests
#pragma warning restore CA1515
{
    private static CategoryTreeSnapshot Real() => CategoryTreeSnapshot.Build(
        InitialCategories.All.Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem)));

    [TestMethod]
    public void QuartoNivel_E_Recusado()
    {
        // Peça de carro (38) está no 3º nível: nada pode nascer abaixo dela
        Assert.AreEqual(CategoryViolation.TooDeep, CategoryRules.Check(Real(), parentId: 38, "Faróis"));
        Assert.AreEqual(CategoryViolation.TooDeep, CategoryRules.Check(Real(), parentId: 42, "Qualquer"));
    }

    [TestMethod]
    public void TerceiroNivel_ESegundoNivel_EPrimeiroNivel_SaoAceitos()
    {
        CategoryTreeSnapshot tree = Real();

        Assert.AreEqual(CategoryViolation.None, CategoryRules.Check(tree, parentId: 3, "Faróis"), "filha de Autopeças = 3º nível");
        Assert.AreEqual(CategoryViolation.None, CategoryRules.Check(tree, parentId: 2, "Bicicletas elétricas"), "2º nível");
        Assert.AreEqual(CategoryViolation.None, CategoryRules.Check(tree, parentId: null, "Turismo"), "1º nível");
    }

    [TestMethod]
    public void NomeRepetidoEntreIrmas_ERecusado_SemDiferencaDeAcentoNemMaiuscula()
    {
        CategoryTreeSnapshot tree = Real();

        Assert.AreEqual(CategoryViolation.DuplicateName, CategoryRules.Check(tree, parentId: 1, "Casas"));
        Assert.AreEqual(CategoryViolation.DuplicateName, CategoryRules.Check(tree, parentId: 1, "  CASAS "));
        Assert.AreEqual(CategoryViolation.DuplicateName, CategoryRules.Check(tree, parentId: 4, "acessorios de celular"), "sem acento");
        Assert.AreEqual(CategoryViolation.DuplicateName, CategoryRules.Check(tree, parentId: null, "imoveis"), "irmãs de primeiro nível");
        Assert.AreEqual(CategoryViolation.DuplicateName, CategoryRules.Check(tree, parentId: 1, "Aluguel   de quartos"), "espaços colapsados");
    }

    [TestMethod]
    public void MesmoNomeEmPaisDiferentes_EAceito()
    {
        CategoryTreeSnapshot tree = Real();

        // "Serviços" é o nome da categoria 7 (primeiro nível) e da 66 (filha dela)
        Assert.AreEqual(CategoryViolation.None, CategoryRules.Check(tree, parentId: 2, "Serviços"));
        Assert.AreEqual(CategoryViolation.None, CategoryRules.Check(tree, parentId: 2, "Casas"));
    }

    [TestMethod]
    public void Renomear_ParaOProprioNome_NaoColideComSiMesma()
    {
        CategoryTreeSnapshot tree = Real();

        Assert.AreEqual(CategoryViolation.None, CategoryRules.Check(tree, parentId: 1, "Casas", ignoreId: 27));
        Assert.AreEqual(CategoryViolation.None, CategoryRules.Check(tree, parentId: 1, "CASAS", ignoreId: 27), "só trocar a caixa");
        Assert.AreEqual(CategoryViolation.DuplicateName, CategoryRules.Check(tree, parentId: 1, "Apartamentos", ignoreId: 27), "mas não pode virar o nome da irmã");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void NomeVazio_ERecusado(string name)
    {
        Assert.AreEqual(CategoryViolation.EmptyName, CategoryRules.Check(Real(), parentId: 1, name));
    }

    [TestMethod]
    public void NomeComMaisDe100Caracteres_ERecusado_ComExatamente100_EAceito()
    {
        Assert.AreEqual(CategoryViolation.NameTooLong, CategoryRules.Check(Real(), parentId: 1, new string('x', 101)));
        Assert.AreEqual(CategoryViolation.None, CategoryRules.Check(Real(), parentId: 1, new string('x', 100)));
    }

    [TestMethod]
    public void PaiInexistente_ERecusado()
    {
        Assert.AreEqual(CategoryViolation.ParentNotFound, CategoryRules.Check(Real(), parentId: 9999, "Qualquer"));
    }

    [TestMethod]
    public void ChaveDeComparacao_IgnoraAcentoCaixaEEspacos()
    {
        Assert.AreEqual(CategoryRules.ComparisonKey("Ônibus"), CategoryRules.ComparisonKey("  onibus "));
        Assert.AreNotEqual(CategoryRules.ComparisonKey("Casa"), CategoryRules.ComparisonKey("Casas"));
    }
}

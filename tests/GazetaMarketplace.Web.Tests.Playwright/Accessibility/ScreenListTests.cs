using System.Linq;
using GazetaMarketplace.Web.Tests.Playwright.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Accessibility;

/// <summary>
/// A lista única de telas ainda é a que os dois testes de navegador (axe e larguras) medem: sem entrada repetida, sem ficha que o E2E não saiba preencher, com tela do painel e do site público. Que toda <b>rota</b>
/// de página do site esteja na lista é conferido pelo teste unitário <c>ScreenCoverageTests</c> (aqui não há o site para descobrir as rotas); este arquivo cuida do lado do navegador. Não precisa de site no ar.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class ScreenListTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void EveryTokenOfTheList_IsOneTheE2EKnowsHowToFill_AndViceVersa()
    {
        string[] fromList = [.. ScreenCatalog.Tokens];

        CollectionAssert.AreEquivalent(fromList, ScreenData.Tokens, "as fichas de screens.json e as que o ScreenData preenche têm de ser as mesmas");
    }

    [TestMethod]
    public void TheList_HasUniqueIds_AndBothKindsOfScreen()
    {
        Assert.AreEqual(ScreenCatalog.All.Count, ScreenCatalog.All.Select(s => s.Id).Distinct().Count(), "id de tela repetido");
        Assert.IsGreaterThanOrEqualTo(15, ScreenCatalog.All.Count(s => s.As == "visitor"), "telas do site público e da entrada do painel");
        Assert.IsGreaterThanOrEqualTo(20, ScreenCatalog.All.Count(s => s.As == "admin"), "telas do painel");
        Assert.IsTrue(ScreenCatalog.All.All(s => s.Prepare is null or "open-filters" or "open-gallery" or "seed-favorites"), "passo de preparo que o ScreenData não conhece");
    }

    [TestMethod]
    public void EveryScreen_IsFoundByTheSameSourceTheTwoSuitesUse()
    {
        if (ScreenCatalog.IsFiltered)
        {
            Assert.Inconclusive("GAZETA_SCREENS está definida: a fonte dos testes mostra só as telas pedidas");
        }

        string[] ids = [.. ScreenCatalog.Ids().Select(d => (string)d[0])];

        CollectionAssert.AreEqual(ScreenCatalog.All.Select(s => s.Id).ToArray(), ids);
    }

    [TestMethod]
    public void TheAxeRules_AreTheWcag21LevelsAAndAA()
    {
        // NFR-16 fala em "0 falhas de nível A ou AA" do WCAG 2.1: se uma etiqueta sair daqui, o axe deixa de olhar uma família de regras (contraste, por exemplo, é de wcag2aa) e nenhuma tela reclamaria
        CollectionAssert.AreEquivalent(new[] { "wcag2a", "wcag2aa", "wcag21a", "wcag21aa" }, AxeHelper.Tags);
    }

    [TestMethod]
    public void TheWidths_AreTheFourOfTheSpec()
    {
        CollectionAssert.AreEqual(new[] { 320, 768, 1024, 1280 }, WidthHelper.Widths);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Ads;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// NFR-16 e NFR-17, a metade que roda sem navegador: a lista única de telas (<c>Screens/screens.json</c>, a mesma que o E2E mede com o axe e nas quatro larguras) tem de acompanhar as rotas
/// reais do site. Toda rota GET que devolve uma página precisa estar na lista (ou na lista de "não é tela", com o motivo); uma rota nova de página sem entrada falha o teste, e uma entrada
/// que aponta para rota que sumiu também. O papel de quem abre a tela tem de bater com a matriz de acesso.
/// </summary>
[TestClass]
public sealed class ScreenCoverageTests
{
    internal sealed record Screen(string Id, string Action, string State, string As, string Site, string Path, int Status, string Prepare);

    internal sealed record NotScreen(string Action, string Reason);

    internal sealed record Catalog(string[] Tokens, NotScreen[] NotScreens, Screen[] Screens);

    internal static Catalog Load()
    {
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Screens", "screens.json"));
        return JsonSerializer.Deserialize<Catalog>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static string ActionOf(AccessMatrixTests.Endpoint e) => $"{e.Controller}.{e.Action}";

    private static async Task<string[]> PageActionsAsync()
    {
        using DraftSite site = await DraftSite.StartAsync();
        (AccessMatrixTests.Endpoint[] endpoints, _) = AccessMatrixTests.Discover(site.Harness.Factory.Services);
        return [.. endpoints.Where(e => !e.IsApi && e.Method is "GET" or "*").Select(ActionOf).Distinct().OrderBy(a => a, StringComparer.Ordinal)];
    }

    [TestMethod]
    public async Task EveryPageRoute_IsInTheScreenList_OrExplainedAsNotAScreen_AndNothingIsStale()
    {
        Catalog catalog = Load();
        string[] actions = await PageActionsAsync();

        string[] listed = [.. catalog.Screens.Select(s => s.Action).Concat(catalog.NotScreens.Select(n => n.Action)).Distinct()];
        string[] missing = [.. actions.Except(listed, StringComparer.Ordinal)];
        string[] stale = [.. listed.Except(actions, StringComparer.Ordinal)];

        Assert.IsEmpty(missing, "rota de página sem tela na lista (acrescente em Screens/screens.json uma entrada por estado, ou em notScreens com o motivo):\n" + string.Join("\n", missing));
        Assert.IsEmpty(stale, "entrada da lista de telas sem rota no site (a rota sumiu ou mudou de nome):\n" + string.Join("\n", stale));
        Assert.IsGreaterThanOrEqualTo(25, actions.Length, "a descoberta achou poucas rotas de página: " + actions.Length);
    }

    [TestMethod]
    public void TheList_IsWellFormed_UniqueIds_KnownTokens_ExplainedExceptions()
    {
        Catalog catalog = Load();

        Assert.IsGreaterThanOrEqualTo(40, catalog.Screens.Length, "a lista tem de cobrir as telas e os estados (vazio, erro, confirmação, negado)");
        Assert.AreEqual(catalog.Screens.Length, catalog.Screens.Select(s => s.Id).Distinct().Count(), "id de tela repetido");
        foreach (Screen screen in catalog.Screens)
        {
            Assert.IsTrue(screen.Path.StartsWith('/') || screen.Path.StartsWith('{'), $"{screen.Id}: o endereço começa com / ou com uma ficha");
            Assert.IsTrue(screen.As is "visitor" or "admin", $"{screen.Id}: 'as' é visitor ou admin");
            Assert.IsTrue(screen.Site is null or "main" or "dev", $"{screen.Id}: 'site' é main ou dev");
            Assert.IsTrue(screen.Status is 200 or 403 or 404, $"{screen.Id}: status esperado 200, 403 ou 404");
            Assert.IsFalse(string.IsNullOrWhiteSpace(screen.State), $"{screen.Id}: diga qual estado a tela mostra");
            foreach (Match token in Regex.Matches(screen.Path, @"\{(\w+)\}"))
            {
                CollectionAssert.Contains(catalog.Tokens, token.Groups[1].Value, $"{screen.Id}: ficha {token.Value} desconhecida");
            }
        }

        Assert.IsTrue(catalog.NotScreens.All(n => n.Reason.Length > 10), "todo 'não é tela' explica o motivo");
        string[] unused = [.. catalog.Tokens.Where(t => !catalog.Screens.Any(s => s.Path.Contains("{" + t + "}", StringComparison.Ordinal)))];
        Assert.IsEmpty(unused, "ficha que nenhuma tela usa: " + string.Join(", ", unused));
    }

    [TestMethod]
    public async Task WhoOpensEachScreen_AgreesWithTheAccessMatrix()
    {
        Catalog catalog = Load();
        using DraftSite site = await DraftSite.StartAsync();
        (AccessMatrixTests.Endpoint[] endpoints, _) = AccessMatrixTests.Discover(site.Harness.Factory.Services);
        Dictionary<string, AccessMatrixTests.Access> access = endpoints.Where(e => e.Method is "GET" or "*").GroupBy(ActionOf).ToDictionary(g => g.Key, g => AccessMatrixTests.Matrix[g.First().Key]);
        List<string> wrong = [];

        foreach (Screen screen in catalog.Screens)
        {
            AccessMatrixTests.Access expected = access[screen.Action];
            bool visitor = expected is AccessMatrixTests.Access.Public or AccessMatrixTests.Access.PanelAnonymous;
            if (visitor != (screen.As == "visitor"))
            {
                wrong.Add($"{screen.Id}: a matriz diz {expected}, a lista abre como {screen.As}");
            }

            if (screen.Path.StartsWith("/painel", StringComparison.Ordinal) && expected is AccessMatrixTests.Access.Public)
            {
                wrong.Add($"{screen.Id}: endereço de painel numa ação pública");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace GazetaMarketplace.Web.Tests.Playwright.Support;

/// <summary>Uma tela da lista única (<c>Screens/screens.json</c>, a mesma que o teste unitário <c>ScreenCoverageTests</c> compara com as rotas reais do site).</summary>
internal sealed record Screen(string Id, string Action, string State, string As, string Site, string Path, int Status, string Prepare)
{
    public bool IsDevelopmentSite => Site == "dev";

    public override string ToString() => Id;
}

internal sealed record ScreenFile(string[] Tokens, Screen[] Screens);

/// <summary>A lista de telas lida do JSON. Alimenta os testes de acessibilidade (axe) e de largura, um caso por tela.</summary>
internal static class ScreenCatalog
{
    private static readonly Lazy<ScreenFile> File = new(() => JsonSerializer.Deserialize<ScreenFile>(
        System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Screens", "screens.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));

    public static IReadOnlyList<Screen> All => File.Value.Screens;

    public static IReadOnlyList<string> Tokens => File.Value.Tokens;

    public static Screen Find(string id) => All.Single(s => s.Id == id);

    /// <summary>Fonte do <c>[DynamicData]</c>: um caso por tela, identificado pelo id.</summary>
    public static IEnumerable<object[]> Ids() => All.Select(s => new object[] { s.Id });

    public static string DisplayName(MethodInfo method, object[] data) => $"{method.Name}({data[0]})";
}

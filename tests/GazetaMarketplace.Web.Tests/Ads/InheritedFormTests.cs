using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>R-06, na tela: o formulário de uma categoria nova sob Imóveis, Roupas, Eletro ou Telefonia mostra os mesmos campos e as mesmas listas do pai (antes, o campo obrigatório de lista vinha vazio).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class InheritedFormTests
#pragma warning restore CA1515
{
    // Nome do campo e quantidade de opções com valor, de cada lista do formulário
    private static string[] Lists(string html) =>
        [.. Regex.Matches(System.Net.WebUtility.HtmlDecode(html), @"<(?:select)[^>]*name=""Fields\[(\w+)\]""[^>]*>(.*?)</select>", RegexOptions.Singleline)
            .Select(m => m.Groups[1].Value + ":" + Regex.Matches(m.Groups[2].Value, @"<option value=""\d+""").Count)];

    private static string[] FieldNames(string html) =>
        [.. Regex.Matches(html, @"name=""Fields\[(\w+)\]").Select(m => m.Groups[1].Value).Distinct()];

    [TestMethod]
    [DataRow(26, "Imóveis")]
    [DataRow(64, "Roupas")]
    [DataRow(128, "Eletro")]
    [DataRow(44, "Telefonia")]
    public async Task FilhaNova_MostraOsMesmosCamposEListasDoPai(int parentId, string label)
    {
        using DraftSite site = await DraftSite.StartAsync(requestsPerMinute: 5000);
        int child = await site.Harness.WithDbAsync(async db =>
        {
            Category created = new() { ParentId = parentId, Name = "Filha de " + label, Slug = "filha-de-" + parentId, DisplayOrder = 99, IsPostable = true };
            db.Categories.Add(created);
            await db.SaveChangesAsync();
            return created.Id;
        });

        string parentHtml = await site.Writer.GetStringAsync($"/painel/anuncios/campos?categoryId={parentId}");
        string childHtml = await site.Writer.GetStringAsync($"/painel/anuncios/campos?categoryId={child}");

        string[] parentLists = Lists(parentHtml);
        Assert.IsNotEmpty(parentLists, label + ": o pai tem listas");
        CollectionAssert.AreEqual(parentLists, Lists(childHtml), label + ": as mesmas listas, com o mesmo número de opções");
        CollectionAssert.AreEqual(FieldNames(parentHtml), FieldNames(childHtml), label + ": os mesmos campos");
        Assert.IsTrue(Lists(childHtml).All(l => !l.EndsWith(":0", System.StringComparison.Ordinal)), label + ": nenhuma lista vazia");
    }
}

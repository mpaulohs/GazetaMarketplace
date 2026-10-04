using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>
/// O formulário é montado por um desenho só para os 9 tipos de campo dos 18 grupos. Estes testes renderizam <b>todos</b> os grupos e todos os tipos de campo
/// e conferem o que importa para quem usa leitor de tela: rótulo ligado ao campo, nome do campo no formulário, ids sem repetição, e a mensagem de erro
/// ligada ao campo por <c>aria-describedby</c>.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FormRenderingTests
#pragma warning restore CA1515
{
    private static async Task<IReadOnlyList<(int CategoryId, FieldGroup Group)>> PostableCategoriesAsync(DraftSite site)
    {
        using IServiceScope scope = site.Harness.Factory.Services.CreateScope();
        CategoryTreeSnapshot tree = await scope.ServiceProvider.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None);
        return [.. tree.All.Where(n => n.IsPostable).Select(n => (n.Id, FieldGroupRegistry.Resolve(tree, n.Id)))];
    }

    private static string[] Ids(string html) => [.. Regex.Matches(html, @"\sid=""([^""]+)""").Select(m => m.Groups[1].Value)];

    [TestMethod]
    public async Task Area_TemAsteriscoDeObrigatorioSoEmTerrenos()
    {
        using DraftSite site = await DraftSite.StartAsync(requestsPerMinute: 5000);
        Regex marked = new(@"for=""campo-areaM2"">\s*Área \(m²\)\s*<span aria-hidden=""true"">\*</span>");

        Assert.IsTrue(marked.IsMatch(System.Net.WebUtility.HtmlDecode(await site.Writer.GetStringAsync("/painel/anuncios/campos?categoryId=30"))), "Terrenos: área obrigatória");
        foreach (int category in new[] { 26, 27, 31 })
        {
            string html = System.Net.WebUtility.HtmlDecode(await site.Writer.GetStringAsync($"/painel/anuncios/campos?categoryId={category}"));
            StringAssert.Contains(html, "campo-areaM2", $"categoria {category} tem o campo");
            Assert.IsFalse(marked.IsMatch(html), $"categoria {category}: área opcional, sem asterisco");
        }
    }

    [TestMethod]
    public async Task TodasAsCategoriasPostaveis_RenderizamOsCamposDoGrupo_ComRotuloENomeEIdsUnicos()
    {
        using DraftSite site = await DraftSite.StartAsync(requestsPerMinute: 5000);
        IReadOnlyList<(int CategoryId, FieldGroup Group)> categories = await PostableCategoriesAsync(site);
        Assert.HasCount(124, categories);
        HashSet<FieldType> typesSeen = [];

        foreach ((int categoryId, FieldGroup group) in categories)
        {
            string html = System.Net.WebUtility.HtmlDecode(await site.Writer.GetStringAsync($"/painel/anuncios/campos?categoryId={categoryId}"));

            foreach (FieldDefinition field in group.FieldsFor(categoryId))
            {
                typesSeen.Add(field.Type);
                string where = $"categoria {categoryId}, campo {field.Key}";
                StringAssert.Contains(html, $"name=\"Fields[{field.Key}]\"", where);
                if (field.Type == FieldType.MultiSelect)
                {
                    Assert.IsTrue(Regex.IsMatch(html, @"<legend[^>]*>\s*" + Regex.Escape(field.Label) + @"\b"), $"legenda do grupo de opções: {where}");
                    int options = field.OptionsFor(categoryId).Options.Count;
                    Assert.AreEqual(options, Regex.Matches(html, $"name=\"Fields\\[{field.Key}\\]\" value=").Count, $"uma caixa por opção: {where}");
                }
                else
                {
                    Assert.IsTrue(Regex.IsMatch(html, $@"<label class=""form-label"" for=""campo-{field.Key}"">\s*{Regex.Escape(field.Label)}"), $"rótulo ligado ao campo: {where}");
                    Assert.IsTrue(Regex.IsMatch(html, $@"<(input|select)[^>]*id=""campo-{field.Key}"""), $"controle com o id do rótulo: {where}");
                }
            }

            string[] ids = Ids(html);
            Assert.AreEqual(ids.Length, ids.Distinct().Count(), $"ids repetidos na categoria {categoryId}: {string.Join(",", ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key))}");
            Assert.AreEqual(group.FieldsFor(categoryId).Count, Regex.Matches(html, @"name=""Fields\[[^\]]+\]""").Select(m => m.Value).Distinct().Count(), $"nem campo a mais nem a menos: categoria {categoryId}");
        }

        CollectionAssert.IsSubsetOf(Enum.GetValues<FieldType>().ToList(), typesSeen.ToList(), "os 9 tipos de campo foram renderizados");
    }

    [TestMethod]
    public async Task RecusaDeCadaCampo_FicaLigadaAoCampoPorAriaDescribedby_EOControleFicaMarcadoComoInvalido()
    {
        using DraftSite site = await DraftSite.StartAsync(requestsPerMinute: 5000);
        IReadOnlyList<(int CategoryId, FieldGroup Group)> categories = await PostableCategoriesAsync(site);
        // Uma categoria de cada grupo e todas as de imóveis (cada uma tem uma lista diferente de campos)
        List<(int CategoryId, FieldGroup Group)> sample =
        [
            .. categories.GroupBy(c => c.Group.Key).Select(g => g.First()),
            .. categories.Where(c => c.Group.Key == FieldGroupKeys.RealEstate)
        ];
        sample = [.. sample.Distinct()];
        HashSet<FieldType> typesChecked = [];

        foreach ((int categoryId, FieldGroup group) in sample)
        {
            List<(string, string)> body = [("Title", "x"), ("CategoryId", categoryId.ToString(System.Globalization.CultureInfo.InvariantCulture))];
            List<FieldDefinition> expectedErrors = [];
            foreach (FieldDefinition field in group.FieldsFor(categoryId))
            {
                string bad = field.Type switch
                {
                    FieldType.Text when field.MaxLength is { } max => new string('x', max + 1),
                    FieldType.Text => null,
                    FieldType.Integer or FieldType.Decimal or FieldType.Money => "abc",
                    FieldType.ModelYear or FieldType.ManufactureYear => "1",
                    _ => "999999"
                };
                if (bad is null)
                {
                    continue;
                }

                body.Add(($"Fields[{field.Key}]", bad));
                expectedErrors.Add(field);
                typesChecked.Add(field.Type);
            }

            HttpResponseMessage response = await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", "/painel/anuncios/novo", [.. body]);
            string page = await DraftSite.BodyAsync(response);

            foreach (FieldDefinition field in expectedErrors)
            {
                string where = $"categoria {categoryId}, campo {field.Key}";
                Assert.IsTrue(Regex.IsMatch(page, $@"<div[^>]*id=""error-campo-{field.Key}""[^>]*role=""alert"">\s*<ul[^>]*>\s*<li>[^<]+</li>"), $"mensagem do campo: {where}");
                Assert.IsTrue(Regex.IsMatch(page, $@"aria-describedby=""[^""]*\berror-campo-{field.Key}\b[^""]*"""), $"aria-describedby aponta para a mensagem: {where}");
                if (field.Type != FieldType.MultiSelect)
                {
                    Assert.IsTrue(Regex.IsMatch(page, $@"id=""campo-{field.Key}""[^>]*aria-invalid=""true"""), $"campo marcado como inválido: {where}");
                }
            }

            // Toda referência de aria-describedby aponta para um id que existe na página
            string[] ids = Ids(page);
            foreach (Match reference in Regex.Matches(page, @"aria-describedby=""([^""]+)"""))
            {
                foreach (string token in reference.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    CollectionAssert.Contains(ids, token, $"aria-describedby \"{token}\" sem elemento: categoria {categoryId}");
                }
            }

            Assert.AreEqual(ids.Length, ids.Distinct().Count(), $"ids repetidos na página com erros: categoria {categoryId}");
        }

        foreach (FieldType type in new[] { FieldType.Text, FieldType.Integer, FieldType.Decimal, FieldType.Money, FieldType.Select, FieldType.MultiSelect, FieldType.ModelYear, FieldType.CatalogItem })
        {
            CollectionAssert.Contains(typesChecked.ToList(), type, $"o tipo {type} foi conferido com erro");
        }
    }

    [TestMethod]
    public async Task PaginaCompleta_ComTodosOsGrupos_TemSoUmH1_ECadaCampoGeralComRotulo()
    {
        using DraftSite site = await DraftSite.StartAsync(requestsPerMinute: 5000);

        string page = await DraftSite.BodyAsync(await site.Writer.GetAsync("/painel/anuncios/novo"));

        Assert.AreEqual(1, Regex.Matches(page, "<h1[ >]").Count);
        foreach (string id in new[] { "categoria", "titulo", "descricao", "preco", "cep" })
        {
            Assert.IsTrue(Regex.IsMatch(page, $@"<label[^>]*for=""{id}"""), $"rótulo de {id}");
            Assert.IsTrue(Regex.IsMatch(page, $@"<(input|select|textarea)[^>]*id=""{id}"""), $"campo {id}");
        }

        string[] ids = Ids(page);
        Assert.AreEqual(ids.Length, ids.Distinct().Count(), "ids repetidos: " + string.Join(",", ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key)));
        StringAssert.Contains(page, "lang=\"pt-BR\"");
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>
/// Campo de número inteiro do formulário (Quilometragem, Horas de uso, Acomoda quantas pessoas): a página manda o navegador aceitar só dígitos, com ponto de milhar,
/// e limita o tamanho ao do maior valor permitido. O servidor continua sendo quem decide (ele recusa letra e valor acima do teto); aqui se garante que a página
/// ajuda a digitar e que ninguém consegue colar um número de 24 dígitos no campo.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class IntegerFieldMaskTests
#pragma warning restore CA1515
{
    private const int CarsCategory = 33;

    private static IEnumerable<string> InputTags(string html) =>
        Regex.Matches(html, @"<input\b[^>]*>").Select(m => m.Value);

    private static string KmInput(string html) => InputTags(html).Single(tag => tag.Contains("id=\"campo-km\"", System.StringComparison.Ordinal));

    [TestMethod]
    public async Task Quilometragem_TemMascaraDeInteiro_ETamanhoLimitadoAoTeto()
    {
        using DraftSite site = await DraftSite.StartAsync(requestsPerMinute: 5000);

        string km = KmInput(await site.Writer.GetStringAsync($"/painel/anuncios/campos?categoryId={CarsCategory}"));

        StringAssert.Contains(km, "data-integer-input");
        StringAssert.Contains(km, "inputmode=\"numeric\"");
        // O teto é 9.999.999 km: 7 dígitos, e com os dois pontos de milhar a máscara escreve 9 caracteres
        StringAssert.Contains(km, "data-max-digits=\"7\"");
        StringAssert.Contains(km, "maxlength=\"9\"");
    }

    [TestMethod]
    public async Task TodoCampoInteiroDeTodosOsGrupos_TemMascaraETamanhoLimitado()
    {
        using DraftSite site = await DraftSite.StartAsync(requestsPerMinute: 5000);
        IReadOnlyList<int> categories;
        using (IServiceScope scope = site.Harness.Factory.Services.CreateScope())
        {
            CategoryTreeSnapshot tree = await scope.ServiceProvider.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None);
            categories = [.. tree.All.Where(n => n.IsPostable).Select(n => n.Id)];
        }

        int checkedFields = 0;
        foreach (int category in categories)
        {
            string html = await site.Writer.GetStringAsync($"/painel/anuncios/campos?categoryId={category}");
            foreach (string tag in InputTags(html).Where(t => t.Contains("inputmode=\"numeric\"", System.StringComparison.Ordinal)))
            {
                checkedFields++;
                StringAssert.Contains(tag, "data-integer-input", $"categoria {category}: {tag}");
                StringAssert.Matches(tag, new Regex(@"data-max-digits=""\d+"""), $"categoria {category}: {tag}");
                StringAssert.Matches(tag, new Regex(@"maxlength=""\d+"""), $"categoria {category}: {tag}");
            }
        }

        Assert.IsGreaterThan(0, checkedFields, "algum grupo tem campo inteiro");
    }
}

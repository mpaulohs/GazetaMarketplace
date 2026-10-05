using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Showcase;

/// <summary>
/// A busca (US-002) no navegador, no site publicado: buscar pela caixa do topo e combinar filtros (S01, S02, S05), faixa de preço invertida (S08), compartilhar o endereço (S09), trocar a UF limpa a
/// cidade (S10), os filtros do bem aparecendo com a categoria, o painel recolhível em 320 px (S12), sem JavaScript e o axe. Três anúncios com um texto único (e preços 3.000, 5.000 e 8.000) isolam a
/// busca dos anúncios que os outros testes deixaram no banco; a conta do E2E (Administrador) os publica pelas telas.
/// Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class SearchE2ETests : SitePage
#pragma warning restore CA1515
{
    private static string _token;
    private static string[] _titles;

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    // Três anúncios com o mesmo texto único, do mais barato ao mais caro; publicados uma vez e reaproveitados
    private async Task<string> TokenAsync()
    {
        if (_token is null)
        {
            using FakeViaCep viaCep = new();
            await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
            string token = "zq" + Guid.NewGuid().ToString("N")[..6];
            List<string> titles = [];
            // O campo de preço da tela do anúncio guarda os dígitos como centavos: 300000 vira R$ 3.000,00
            foreach (string price in new[] { "300000", "500000", "800000" })
            {
                titles.Add(await PublishingFlow.PublishAsync(Page, $"{token} livro azul", 1, price: price).ConfigureAwait(false));
            }

            _titles = [.. titles];
            _token = token;
        }

        return _token;
    }

    private async Task<IPage> VisitorAsync(string path, int? width = null, bool javaScript = true)
    {
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, JavaScriptEnabled = javaScript }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);
        if (width is { } w)
        {
            await visitor.SetViewportSizeAsync(w, 800).ConfigureAwait(false);
        }

        await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
        return visitor;
    }

    private static ILocator Total(IPage page) => page.Locator("[data-search-total]");

    private static ILocator Cards(IPage page) => page.Locator("[data-ad-card]");

    private static ILocator Apply(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Aplicar filtros" });

    private static ILocator Select(IPage page, string label) => page.GetByLabel(label, new() { Exact = true });

    // Os preços na ordem em que os cards aparecem: "R$ 3.000", "R$ 5.000"...
    private static async Task<string[]> CardPricesAsync(IPage page) =>
        [.. (await Cards(page).AllInnerTextsAsync().ConfigureAwait(false)).Select(t => Regex.Match(t, @"R\$\s*[\d.]+").Value)];

    private static async Task<bool> HasHorizontalScrollAsync(IPage page) =>
        await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false);

    [TestMethod]
    public async Task US002S01_S02_S05_BuscarPelaCaixaDoTopo_CombinarFiltros_EOrdenar()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync("/").ConfigureAwait(false);

        // S01: o texto na caixa do topo
        await visitor.GetByRole(AriaRole.Searchbox, new() { Name = "Buscar anúncios" }).FillAsync(token.ToUpperInvariant()).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Buscar" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"/busca\?q=")).ConfigureAwait(false);
        await Expect(Total(visitor)).ToHaveTextAsync("3 anúncios encontrados").ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { "R$ 8.000", "R$ 5.000", "R$ 3.000" }, await CardPricesAsync(visitor).ConfigureAwait(false), "mais recentes primeiro (o último publicado é o de 8.000)");

        // S02: preço máximo e UF
        await Select(visitor, "UF").SelectOptionAsync("SP").ConfigureAwait(false);
        await visitor.GetByLabel("Preço máximo").FillAsync("5000").ConfigureAwait(false);
        await Apply(visitor).ClickAsync().ConfigureAwait(false);
        await Expect(Total(visitor)).ToHaveTextAsync("2 anúncios encontrados").ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"uf=SP")).ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"precoMax=5000")).ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"q=" + token, RegexOptions.IgnoreCase)).ConfigureAwait(false);
        Assert.IsFalse(Regex.IsMatch(visitor.Url, @"[?&](precoMin|cidade|marca|categoria)=&|=$"), "campo vazio não vai para o endereço: " + visitor.Url);
        foreach (string card in await Cards(visitor).AllInnerTextsAsync().ConfigureAwait(false))
        {
            StringAssert.Contains(card, "Campinas/SP");
        }

        // S05: ordenar escolhendo a ordem (envia sozinho) e a ordem continua escolhida
        await Select(visitor, "Ordenar").SelectOptionAsync("menor-preco").ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"ordem=menor-preco")).ConfigureAwait(false);
        await Expect(Total(visitor)).ToHaveTextAsync("2 anúncios encontrados").ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { "R$ 3.000", "R$ 5.000" }, await CardPricesAsync(visitor).ConfigureAwait(false));
        await Expect(Select(visitor, "Ordenar")).ToHaveValueAsync("menor-preco").ConfigureAwait(false);
        await Expect(Select(visitor, "UF")).ToHaveValueAsync("SP").ConfigureAwait(false);
        await Expect(visitor.GetByLabel("Preço máximo")).ToHaveValueAsync("5000").ConfigureAwait(false);

        // Maior preço
        await Select(visitor, "Ordenar").SelectOptionAsync("maior-preco").ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"ordem=maior-preco")).ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { "R$ 5.000", "R$ 3.000" }, await CardPricesAsync(visitor).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task US002S07_BuscaSemResultados_MensagemELimparFiltrosVoltaATudo()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}&uf=RJ").ConfigureAwait(false);

        await Expect(visitor.GetByText("Nenhum anúncio encontrado para esses filtros")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Cards(visitor)).ToHaveCountAsync(0).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Link, new() { Name = "Limpar filtros" }).First.ClickAsync().ConfigureAwait(false);

        await Expect(visitor).ToHaveURLAsync(Url("/busca")).ConfigureAwait(false);
        await Expect(Cards(visitor).First).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Select(visitor, "UF")).ToHaveValueAsync(string.Empty).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Searchbox, new() { Name = "Buscar anúncios" })).ToHaveValueAsync(string.Empty).ConfigureAwait(false);
        StringAssert.Matches(await Total(visitor).InnerTextAsync().ConfigureAwait(false), new Regex(@"^\d[\d.]* anúncios? encontrados?$"), "todos os publicados");
    }

    [TestMethod]
    public async Task US002S08_FaixaInvertida_ComJavaScript_ErroJuntoDoCampo_EAListaNaoMuda()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}").ConfigureAwait(false);
        await Expect(Total(visitor)).ToHaveTextAsync("3 anúncios encontrados").ConfigureAwait(false);
        string before = visitor.Url;

        await visitor.GetByLabel("Preço mínimo").FillAsync("5000").ConfigureAwait(false);
        await visitor.GetByLabel("Preço máximo").FillAsync("1000").ConfigureAwait(false);
        await Apply(visitor).ClickAsync().ConfigureAwait(false);

        await Expect(visitor.GetByRole(AriaRole.Alert).Filter(new() { HasText = "O preço mínimo não pode ser maior que o máximo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByLabel("Preço mínimo")).ToHaveAttributeAsync("aria-invalid", "true").ConfigureAwait(false);
        Assert.AreEqual(before, visitor.Url, "o envio foi bloqueado: nada mudou no endereço");
        await Expect(Total(visitor)).ToHaveTextAsync("3 anúncios encontrados").ConfigureAwait(false);
        Assert.AreEqual(3, await Cards(visitor).CountAsync().ConfigureAwait(false), "a lista continua igual");

        // Corrigindo o valor, o erro some e a busca vale
        await visitor.GetByLabel("Preço mínimo").FillAsync("1000").ConfigureAwait(false);
        await visitor.GetByLabel("Preço máximo").FillAsync("4000").ConfigureAwait(false);
        await Expect(visitor.GetByText("O preço mínimo não pode ser maior que o máximo")).ToBeHiddenAsync().ConfigureAwait(false);
        await Apply(visitor).ClickAsync().ConfigureAwait(false);
        await Expect(Total(visitor)).ToHaveTextAsync("1 anúncio encontrado").ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { "R$ 3.000" }, await CardPricesAsync(visitor).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task US002S08_FaixaInvertida_SemJavaScript_ServidorMostraOErro_EListaSemAFaixa()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}", javaScript: false).ConfigureAwait(false);

        await visitor.GetByLabel("Preço mínimo").FillAsync("5000").ConfigureAwait(false);
        await visitor.GetByLabel("Preço máximo").FillAsync("1000").ConfigureAwait(false);
        await Apply(visitor).ClickAsync().ConfigureAwait(false);

        await Expect(visitor.GetByRole(AriaRole.Alert).Filter(new() { HasText = "O preço mínimo não pode ser maior que o máximo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Total(visitor)).ToHaveTextAsync("3 anúncios encontrados").ConfigureAwait(false);
        await Expect(visitor.GetByLabel("Preço mínimo")).ToHaveValueAsync("5000").ConfigureAwait(false);
        await Expect(visitor.GetByLabel("Preço máximo")).ToHaveValueAsync("1000").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US002S09_CompartilharPeloEndereco_OutraAbaVeOsMesmosFiltrosEOsMesmosResultados()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}").ConfigureAwait(false);
        await Select(visitor, "UF").SelectOptionAsync("SP").ConfigureAwait(false);
        await visitor.GetByLabel("Preço máximo").FillAsync("8000").ConfigureAwait(false);
        await Apply(visitor).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"precoMax=8000")).ConfigureAwait(false); // espera a página nova antes de mexer na ordem: senão a escolha cai na página que está saindo
        await Select(visitor, "Ordenar").SelectOptionAsync("menor-preco").ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"ordem=menor-preco")).ConfigureAwait(false);
        string[] expected = await CardPricesAsync(visitor).ConfigureAwait(false);
        string shared = visitor.Url;

        IPage otherTab = await VisitorAsync(new Uri(shared).PathAndQuery).ConfigureAwait(false);

        await Expect(Select(otherTab, "UF")).ToHaveValueAsync("SP").ConfigureAwait(false);
        await Expect(otherTab.GetByLabel("Preço máximo")).ToHaveValueAsync("8000").ConfigureAwait(false);
        await Expect(Select(otherTab, "Ordenar")).ToHaveValueAsync("menor-preco").ConfigureAwait(false);
        await Expect(otherTab.GetByRole(AriaRole.Searchbox, new() { Name = "Buscar anúncios" })).ToHaveValueAsync(token).ConfigureAwait(false);
        CollectionAssert.AreEqual(expected, await CardPricesAsync(otherTab).ConfigureAwait(false), "os mesmos resultados na mesma ordem");
        Assert.AreEqual(shared, otherTab.Url, "o endereço não muda ao reabrir");
    }

    [TestMethod]
    public async Task US002S10_TrocarAUfLimpaACidade_EAListaPassaAMostrarSoAsCidadesDaNovaUf()
    {
        IPage visitor = await VisitorAsync("/busca").ConfigureAwait(false);
        ILocator city = Select(visitor, "Cidade");
        await Expect(city).ToBeDisabledAsync().ConfigureAwait(false);

        await Select(visitor, "UF").SelectOptionAsync("SP").ConfigureAwait(false);
        await Expect(city).ToBeEnabledAsync().ConfigureAwait(false);
        await Expect(city.Locator("option", new() { HasText = "Campinas" })).ToHaveCountAsync(1).ConfigureAwait(false);
        await city.SelectOptionAsync(new SelectOptionValue { Label = "Campinas" }).ConfigureAwait(false);
        await Expect(city).ToHaveValueAsync("Campinas").ConfigureAwait(false);

        await Select(visitor, "UF").SelectOptionAsync("RJ").ConfigureAwait(false);

        await Expect(city.Locator("option", new() { HasText = "Rio de Janeiro" })).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(city).ToHaveValueAsync(string.Empty).ConfigureAwait(false);
        await Expect(city.Locator("option:checked")).ToHaveTextAsync("Todas as cidades").ConfigureAwait(false);
        await Expect(city.Locator("option", new() { HasText = "Campinas" })).ToHaveCountAsync(0).ConfigureAwait(false);

        // Sem UF: a cidade volta a ficar desabilitada e vazia (a cidade escolhida antes não fica guardada num campo travado)
        await city.SelectOptionAsync(new SelectOptionValue { Label = "Rio de Janeiro" }).ConfigureAwait(false);
        await Select(visitor, "UF").SelectOptionAsync(string.Empty).ConfigureAwait(false);
        await Expect(city).ToBeDisabledAsync().ConfigureAwait(false);
        await Expect(city).ToHaveValueAsync(string.Empty).ConfigureAwait(false);
        await Expect(city.Locator("option")).ToHaveCountAsync(1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US002S10_ComCidadeEscolhida_ABuscaFiltraPelaCidade()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}").ConfigureAwait(false);
        await Select(visitor, "UF").SelectOptionAsync("SP").ConfigureAwait(false);
        await Expect(Select(visitor, "Cidade").Locator("option", new() { HasText = "Campinas" })).ToHaveCountAsync(1).ConfigureAwait(false);
        await Select(visitor, "Cidade").SelectOptionAsync(new SelectOptionValue { Label = "Campinas" }).ConfigureAwait(false);

        await Apply(visitor).ClickAsync().ConfigureAwait(false);

        await Expect(Total(visitor)).ToHaveTextAsync("3 anúncios encontrados").ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"cidade=Campinas")).ConfigureAwait(false);
        await Expect(Select(visitor, "Cidade")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US002S03_S04_OsFiltrosDoBemAparecemSoComACategoriaCerta_EMarcaCarregaOsModelos()
    {
        IPage visitor = await VisitorAsync("/busca").ConfigureAwait(false);
        ILocator brand = Select(visitor, "Marca");
        ILocator model = Select(visitor, "Modelo");
        ILocator kmMax = visitor.GetByLabel("Quilometragem máx. (km)");
        ILocator areaMin = visitor.GetByLabel("Área mínima");

        await Expect(brand).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(kmMax).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(areaMin).ToBeHiddenAsync().ConfigureAwait(false);

        // Carros: marca, modelo, ano e quilometragem
        await Select(visitor, "Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Carros, vans e utilitários" }).ConfigureAwait(false);
        await Expect(brand).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(model).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(model).ToBeDisabledAsync().ConfigureAwait(false);
        await Expect(visitor.GetByLabel("Ano inicial")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(kmMax).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(areaMin).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(brand.Locator("option", new() { HasText = "Honda" })).ToHaveCountAsync(1).ConfigureAwait(false);

        await brand.SelectOptionAsync(new SelectOptionValue { Label = "Honda" }).ConfigureAwait(false);
        await Expect(model).ToBeEnabledAsync().ConfigureAwait(false);
        await Expect(model.Locator("option", new() { HasText = "Civic" })).ToHaveCountAsync(1).ConfigureAwait(false);

        // Trocar a marca esvazia o modelo
        await model.SelectOptionAsync(new SelectOptionValue { Label = "Civic" }).ConfigureAwait(false);
        await brand.SelectOptionAsync(new SelectOptionValue { Label = "Toyota" }).ConfigureAwait(false);
        await Expect(model).ToHaveValueAsync(string.Empty).ConfigureAwait(false);
        await Expect(model.Locator("option", new() { HasText = "Civic" })).ToHaveCountAsync(0).ConfigureAwait(false);

        // Terrenos: só a área
        await Select(visitor, "Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Terrenos, sítios e fazendas" }).ConfigureAwait(false);
        await Expect(areaMin).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByLabel("Área máxima")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(brand).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(kmMax).ToBeHiddenAsync().ConfigureAwait(false);

        // Livros: nenhum filtro específico
        await Select(visitor, "Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
        await Expect(areaMin).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(visitor.GetByLabel("Ano inicial")).ToBeHiddenAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US002S03_VeiculosDeVerdade_MarcaAnoEQuilometragemFiltramNoEndereco_ECategoriaNovaTiraOQueNaoServe()
    {
        IPage visitor = await VisitorAsync("/busca").ConfigureAwait(false);
        await Select(visitor, "Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Carros, vans e utilitários" }).ConfigureAwait(false);
        await Select(visitor, "Marca").SelectOptionAsync(new SelectOptionValue { Label = "Honda" }).ConfigureAwait(false);
        await visitor.GetByLabel("Ano inicial").FillAsync("2015").ConfigureAwait(false);
        await visitor.GetByLabel("Ano final").FillAsync("2020").ConfigureAwait(false);
        await visitor.GetByLabel("Quilometragem máx. (km)").FillAsync("100000").ConfigureAwait(false);

        await Apply(visitor).ClickAsync().ConfigureAwait(false);

        await Expect(visitor).ToHaveURLAsync(new Regex(@"categoria=cars")).ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"marca=\d+")).ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"anoDe=2015")).ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"anoAte=2020")).ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"kmMax=100000")).ConfigureAwait(false);
        await Expect(Select(visitor, "Marca").Locator("option:checked")).ToHaveTextAsync("Honda").ConfigureAwait(false);
        await Expect(visitor.GetByLabel("Ano inicial")).ToHaveValueAsync("2015").ConfigureAwait(false);

        // Trocar para uma categoria sem filtro de veículo: os campos somem e o que valia deixa de ir no endereço
        await Select(visitor, "Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
        await Apply(visitor).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"categoria=livros-e-revistas")).ConfigureAwait(false);
        Assert.IsFalse(Regex.IsMatch(visitor.Url, @"marca=|anoDe=|anoAte=|kmMax="), "filtros de veículo não vão com outra categoria: " + visitor.Url);
    }

    [TestMethod]
    public async Task Paridade_LerNumeroDoJavaScript_DaOMesmoValorQueODecimalInputDoServidor_NaMesmaTabela()
    {
        // A mesma tabela do DecimalInputTests (C#): (o que se digita, o valor esperado ou nulo se ilegível). A conferência de faixa do navegador e a do servidor não podem discordar.
        (string Typed, string Expected)[] table =
        [
            ("50000", "50000"),
            ("50.000", "50000"),
            ("50.000,00", "50000"),
            ("50000,5", "50000.5"),
            ("50000.50", "50000.5"),
            ("R$ 1.234,56", "1234.56"),
            ("R$1.234,56", "1234.56"),
            ("r$ 5", "5"),
            ("0", "0"),
            ("0,01", "0.01"),
            ("007", "7"),
            ("1.234", "1234"),
            ("1.23", "1.23"),
            ("12.345.678,9", "12345678.9"),
            (" 42 ", "42"),
            ("\t42\n", "42"),
            ("99999999,99", "99999999.99"),
            ("", null),
            ("   ", null),
            ("R$", null),
            ("abc", null),
            ("-5", null),
            ("+5", null),
            ("1,2,3", null),
            ("50.00.0", null),
            ("50000,123", null),
            ("1e3", null),
            ("5 000", null),
            ("1.2345", null),
            (".5", null),
            ("5.", null),
            (",5", null),
            ("1.000.00", null),
            ("12,3.4", null),
            ("1,234.56", null),
            ("٣٠", null),
            ("5\u00a0000", null),
            ("5,", null),
            ("1..000", null),
            ("1.000,", null)
        ];
        IPage visitor = await VisitorAsync("/busca").ConfigureAwait(false);

        double?[] results = await visitor.EvaluateAsync<double?[]>(
            "async (typed) => { const { lerNumero } = await import('/js/pages/search.js'); return typed.map((t) => { const n = lerNumero(t); return Number.isNaN(n) ? null : n; }); }",
            table.Select(row => row.Typed).ToArray()).ConfigureAwait(false);

        Assert.AreEqual(table.Length, results.Length);
        for (int i = 0; i < table.Length; i++)
        {
            double? expected = table[i].Expected is null ? null : double.Parse(table[i].Expected, System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, results[i], $"\"{table[i].Typed}\"");
        }
    }

    [TestMethod]
    public async Task US002S12_Celular320px_PainelRecolhidoAbreEFecha_SemRolagemHorizontal()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}", 320).ConfigureAwait(false);
        ILocator toggle = visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Filtros") });
        ILocator panel = visitor.Locator("#filtros");

        await Expect(toggle).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(panel).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false").ConfigureAwait(false);
        Assert.IsFalse(await HasHorizontalScrollAsync(visitor).ConfigureAwait(false), "sem rolagem horizontal com o painel fechado");
        await Expect(Cards(visitor).First).ToBeVisibleAsync().ConfigureAwait(false);

        await toggle.ClickAsync().ConfigureAwait(false);
        await Expect(panel).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(panel).ToHaveClassAsync(new Regex(@"\bcollapse show\b")).ConfigureAwait(false); // a animação terminou: o Bootstrap ignora um clique no meio dela
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true").ConfigureAwait(false);
        await Expect(Apply(visitor)).ToBeVisibleAsync().ConfigureAwait(false);
        Assert.IsFalse(await HasHorizontalScrollAsync(visitor).ConfigureAwait(false), "sem rolagem horizontal com o painel aberto");
        foreach (ILocator control in new[] { Select(visitor, "Categoria"), Select(visitor, "UF"), visitor.GetByLabel("Preço máximo"), Apply(visitor) })
        {
            LocatorBoundingBoxResult box = await control.BoundingBoxAsync().ConfigureAwait(false);
            Assert.IsTrue(box.X >= 0 && box.X + box.Width <= 320, $"dentro dos 320 px: {box.X}+{box.Width}");
        }

        await visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Fechar") }).ClickAsync().ConfigureAwait(false);
        await Expect(panel).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(toggle).ToBeFocusedAsync().ConfigureAwait(false);

        // Os resultados rolam só para baixo
        await Expect(Total(visitor)).ToBeVisibleAsync().ConfigureAwait(false);
        Assert.IsFalse(await HasHorizontalScrollAsync(visitor).ConfigureAwait(false));
        await Expect(Select(visitor, "Ordenar")).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US002S12_Celular_ComErroNoFiltro_OPainelNaoRecolhe()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}&precoMin=5000&precoMax=1000", 320).ConfigureAwait(false);

        await Expect(visitor.Locator("#filtros")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Alert).Filter(new() { HasText = "O preço mínimo não pode ser maior que o máximo" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Desktop_PainelDeFiltrosAbertoAoLado_SemBotaoDeRecolher()
    {
        IPage visitor = await VisitorAsync("/busca", 1280).ConfigureAwait(false);

        await Expect(visitor.Locator("#filtros")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Filtros") })).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Fechar") })).ToBeHiddenAsync().ConfigureAwait(false);
        LocatorBoundingBoxResult filters = await visitor.Locator("#filtros").BoundingBoxAsync().ConfigureAwait(false);
        LocatorBoundingBoxResult results = await Total(visitor).BoundingBoxAsync().ConfigureAwait(false);
        Assert.IsTrue(results.X >= filters.X + filters.Width - 1, "os resultados ficam à direita do painel");
    }

    [TestMethod]
    public async Task SemJavaScript_OFormularioFunciona_ComPainelAbertoENenhumBotaoInutil()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}", 320, javaScript: false).ConfigureAwait(false);

        await Expect(visitor.Locator("#filtros")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Fechar") })).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Button, new() { Name = "Ordenar" })).ToBeVisibleAsync().ConfigureAwait(false);

        await visitor.GetByLabel("Preço máximo").FillAsync("5000").ConfigureAwait(false);
        await Apply(visitor).ClickAsync().ConfigureAwait(false);
        await Expect(Total(visitor)).ToHaveTextAsync("2 anúncios encontrados").ConfigureAwait(false);

        await Select(visitor, "Ordenar").SelectOptionAsync("menor-preco").ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Ordenar" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"ordem=menor-preco")).ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { "R$ 3.000", "R$ 5.000" }, await CardPricesAsync(visitor).ConfigureAwait(false));
        Assert.IsTrue(visitor.Url.Contains("precoMax=5000", StringComparison.Ordinal), "o Ordenar leva os filtros");

        // Sem JavaScript, a UF escolhida só libera a cidade depois de aplicar
        await Select(visitor, "UF").SelectOptionAsync("SP").ConfigureAwait(false);
        await Apply(visitor).ClickAsync().ConfigureAwait(false);
        await Expect(Select(visitor, "Cidade")).ToBeEnabledAsync().ConfigureAwait(false);
        await Expect(Select(visitor, "Cidade").Locator("option", new() { HasText = "Campinas" })).ToHaveCountAsync(1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Enviar_BotaoMostraBuscandoEFicaDesabilitado_CampoVazioNaoVaiNoEndereco_EVoltarRestauraOBotao()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}").ConfigureAwait(false);
        await visitor.GetByLabel("Preço máximo").FillAsync("5000").ConfigureAwait(false);

        // O envio é cancelado logo depois do tratamento da página (um "preventDefault" registrado depois do dela), para olhar o botão no meio da espera
        string during = await visitor.EvaluateAsync<string>(@"() => {
            const form = document.querySelector('[data-search-form]');
            form.addEventListener('submit', (e) => e.preventDefault(), { once: true });
            form.requestSubmit();
            const button = form.querySelector('[data-search-submit]');
            const sent = [...new FormData(form).keys()].join(',');
            return button.textContent + '|' + button.disabled + '|' + sent;
        }").ConfigureAwait(false);

        string[] parts = during.Split('|');
        Assert.AreEqual("Buscando…", parts[0]);
        Assert.AreEqual("true", parts[1], "o botão fica desabilitado enquanto a busca anda");
        Assert.AreEqual("q,precoMax", parts[2], "só vão no endereço os campos preenchidos");
        await Expect(visitor.GetByRole(AriaRole.Button, new() { Name = "Buscando…" })).ToBeDisabledAsync().ConfigureAwait(false);

        // Voltar pelo botão do navegador pode trazer a página de volta como estava: o botão não pode ficar travado
        await visitor.EvaluateAsync("() => window.dispatchEvent(new PageTransitionEvent('pageshow', { persisted: true }))").ConfigureAwait(false);
        await Expect(Apply(visitor)).ToBeEnabledAsync().ConfigureAwait(false);
        await Expect(Select(visitor, "UF")).ToBeEnabledAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Enviar_DeVerdade_OEnderecoSaiEnxuto_SemParametroVazio()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={token}").ConfigureAwait(false);
        await visitor.GetByLabel("Preço máximo").FillAsync("5000").ConfigureAwait(false);

        await Apply(visitor).ClickAsync().ConfigureAwait(false);

        await Expect(Total(visitor)).ToHaveTextAsync("2 anúncios encontrados").ConfigureAwait(false);
        Assert.AreEqual($"/busca?q={token}&precoMax=5000", new Uri(visitor.Url).PathAndQuery);
        await Expect(Apply(visitor)).ToBeEnabledAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PaginaDeCategoria_FiltrarEOrdenar_LevaABuscaComACategoriaEscolhida()
    {
        IPage visitor = await VisitorAsync("/categoria/livros-e-revistas").ConfigureAwait(false);

        await visitor.GetByRole(AriaRole.Link, new() { Name = "Filtrar e ordenar" }).ClickAsync().ConfigureAwait(false);

        await Expect(visitor).ToHaveURLAsync(Url("/busca?categoria=livros-e-revistas")).ConfigureAwait(false);
        await Expect(Select(visitor, "Categoria").Locator("option:checked")).ToContainTextAsync("Livros e revistas").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_Busca_SemViolacoes_EmDesktopECelular_ComFiltrosDeVeiculoEErro()
    {
        string token = await TokenAsync().ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };
        string[] paths = [$"/busca?q={token}", "/busca?categoria=cars&marca=1&anoDe=2015", $"/busca?q={token}&precoMin=5000&precoMax=1000", "/busca?q=xyzabcnaoexiste", "/busca?categoria=terrenos-sitios-e-fazendas"];

        foreach (int width in new[] { 1280, 320 })
        {
            foreach (string path in paths)
            {
                IPage visitor = await VisitorAsync(path, width).ConfigureAwait(false);
                await visitor.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
                if (width == 320)
                {
                    // O painel recolhido não tem o que verificar; aberto, o axe vê os campos
                    ILocator toggle = visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Filtros") });
                    if (await toggle.GetAttributeAsync("aria-expanded").ConfigureAwait(false) == "false")
                    {
                        await toggle.ClickAsync().ConfigureAwait(false);
                        await Expect(visitor.Locator("#filtros")).ToBeVisibleAsync().ConfigureAwait(false);
                    }
                }

                AxeResult result = await visitor.RunAxe(options).ConfigureAwait(false);
                Assert.AreEqual(0, result.Violations.Length, $"{path} em {width}px: " + string.Join("; ", result.Violations.Select(Describe)));
            }
        }
    }
}

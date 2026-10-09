using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Ads;

/// <summary>
/// O formulário do anúncio (US-008) no navegador (roda no /test): a jornada de salvar e reabrir, a troca de categoria sem recarregar, a máscara de
/// preço, os contadores, o serviço de CEP fora do ar e o mesmo caminho sem JavaScript. Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD
/// (conta de Administrador) e GAZETA_E2E_VIACEP_PORT (porta do ViaCEP de mentira que o teste abre e para onde o site manda as consultas). O banco do E2E
/// precisa ter o catálogo de exemplo (<c>db/seed/sample/vehicle-catalog-sample.sql</c>). Cada teste cria os próprios rascunhos com título único.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class DraftE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string Cars = "Carros, vans e utilitários";

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html)) + "]";

    /// <summary>ViaCEP de mentira: 13015100 existe, 99999999 não existe e 50000000 está fora do ar.</summary>
    private sealed class FakeViaCep : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly ConcurrentDictionary<string, int> _hits = new();

        public FakeViaCep()
        {
            int port = int.Parse(RequiresVariablesAttribute.Value("GAZETA_E2E_VIACEP_PORT"), System.Globalization.CultureInfo.InvariantCulture);
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _ = Task.Run(LoopAsync);
        }

        public int Hits(string cep) => _hits.TryGetValue(cep, out int n) ? n : 0;

        private async Task LoopAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    return;
                }

                Match match = Regex.Match(context.Request.Url!.AbsolutePath, @"^/ws/(\d{8})/json/$");
                string cep = match.Success ? match.Groups[1].Value : string.Empty;
                _hits.AddOrUpdate(cep, 1, (_, n) => n + 1);
                (int status, string body) = cep switch
                {
                    "13015100" => (200, """{"cep":"13015-100","localidade":"Campinas","uf":"SP","ibge":"3509502"}"""),
                    "99999999" => (200, """{"erro":true}"""),
                    _ => (500, "falhou")
                };
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(body)).ConfigureAwait(false);
                context.Response.Close();
            }
        }

        public void Dispose()
        {
            _stop.Cancel();
            _listener.Close();
        }
    }

    private static async Task SignInAsync(IPage page)
    {
        await page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
    }

    private static async Task OpenNewAsync(IPage page)
    {
        await page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Novo anúncio", Exact = true }).WaitForAsync().ConfigureAwait(false);
    }

    /// <summary>Guarda as respostas de erro do próprio site durante a jornada (rede de segurança: uma jornada feliz não pode ter nenhuma).</summary>
    private static List<string> WatchFailures(IPage page)
    {
        List<string> failures = [];
        string origin = new Uri(RequiresVariablesAttribute.Value("GAZETA_BASE_URL")).GetLeftPart(UriPartial.Authority);
        page.Response += (_, response) =>
        {
            if (response.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase) && response.Status >= 400)
            {
                failures.Add($"{response.Status} {response.Url}");
            }
        };
        return failures;
    }

    private ILocator Category() => Page.GetByLabel("Categoria");

    private async Task ChooseCategoryAsync(string label)
    {
        await Category().SelectOptionAsync(new SelectOptionValue { Label = label }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US008S01_JornadaCompleta_SalvaEReabreComTodosOsCampos_SemErroDeRede()
    {
        using FakeViaCep viaCep = new();
        List<string> failures = WatchFailures(Page);
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenNewAsync(Page).ConfigureAwait(false);
        string title = Unique("Honda Civic 2018");

        await Page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        await Page.GetByLabel("Descrição").FillAsync("Único dono\nRevisões na concessionária").ConfigureAwait(false);
        await ChooseCategoryAsync(Cars).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Quilometragem")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByLabel("Preço").FillAsync("R$ 62.000,00").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Preço")).ToHaveValueAsync("62.000,00").ConfigureAwait(false);

        await Page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        await Expect(Page.GetByLabel("UF (automático)")).ToHaveValueAsync("SP").ConfigureAwait(false);

        await Page.GetByLabel("Marca").SelectOptionAsync(new SelectOptionValue { Label = "Honda" }).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Modelo")).ToBeEnabledAsync().ConfigureAwait(false);
        await Page.GetByLabel("Modelo").SelectOptionAsync(new SelectOptionValue { Label = "Civic" }).ConfigureAwait(false);
        await Expect(Page.GetByLabel(new Regex(@"^Ano\b"))).ToBeEnabledAsync().ConfigureAwait(false);
        await Page.GetByLabel(new Regex(@"^Ano\b")).SelectOptionAsync(new SelectOptionValue { Label = "2018" }).ConfigureAwait(false);
        await Page.GetByLabel("Quilometragem").FillAsync("45000").ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Rascunho salvo" })).ToBeVisibleAsync().ConfigureAwait(false);

        // Recarrega a página: só o que o servidor guardou pode aparecer
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Título")).ToHaveValueAsync(title).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Descrição")).ToHaveValueAsync("Único dono\nRevisões na concessionária").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Preço")).ToHaveValueAsync("62.000,00").ConfigureAwait(false);
        await Expect(Category()).ToHaveValueAsync(new Regex(@"^\d+$")).ConfigureAwait(false);
        await Expect(Page.GetByLabel("CEP")).ToHaveValueAsync("13015-100").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Marca")).ToHaveValueAsync("1").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Modelo")).ToHaveValueAsync("1").ConfigureAwait(false);
        await Expect(Page.GetByLabel(new Regex(@"^Ano\b"))).ToHaveValueAsync("2018").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Quilometragem")).ToHaveValueAsync("45000").ConfigureAwait(false);
        // O cache de 30 dias do servidor pode já ter o CEP de uma rodada anterior; de todo modo nunca mais que uma consulta: o navegador resolve e o servidor só confere
        Assert.IsLessThanOrEqualTo(1, viaCep.Hits("13015100"), "o servidor não repete a consulta que o navegador já fez");
        Assert.IsEmpty(failures, "respostas de erro do site na jornada: " + string.Join("; ", failures));
    }

    [TestMethod]
    public async Task US008S09_TrocarDeCategoria_RefazOsCamposSemRecarregar_MantemOComum_EDevolveOFoco()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenNewAsync(Page).ConfigureAwait(false);
        await Page.EvaluateAsync("window.__marcador = 'mesma-pagina'").ConfigureAwait(false);
        await Page.GetByLabel("Título").FillAsync("Meu anúncio").ConfigureAwait(false);
        await Page.GetByLabel("Descrição").FillAsync("Texto que deve continuar").ConfigureAwait(false);
        await Page.GetByLabel("Preço").FillAsync("1500").ConfigureAwait(false); // a máscara deixa "15,00"

        await ChooseCategoryAsync(Cars).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Marca")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Quilometragem")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Category()).ToBeFocusedAsync().ConfigureAwait(false);

        await ChooseCategoryAsync("Terrenos, sítios e fazendas").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Área (m²)")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Quilometragem")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Marca")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Tipo")).ToBeVisibleAsync().ConfigureAwait(false);

        await ChooseCategoryAsync("Livros e revistas").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Condição")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Área (m²)")).ToHaveCountAsync(0).ConfigureAwait(false);

        await ChooseCategoryAsync("Vagas de emprego").ConfigureAwait(false);
        await Expect(Page.GetByText("Vagas de emprego não têm fotos")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Título")).ToHaveAttributeAsync("maxlength", "90").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Informações adicionais")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("0/90").Or(Page.GetByText("11/90"))).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Salário")).ToHaveValueAsync("15,00").ConfigureAwait(false);

        await ChooseCategoryAsync("Serviços").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Preço")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByText("Este anúncio aceita até 6 fotos")).ToBeVisibleAsync().ConfigureAwait(false);

        // O que é comum continuou preenchido o tempo todo, e a página nunca foi recarregada
        await Expect(Page.GetByLabel("Título")).ToHaveValueAsync("Meu anúncio").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Informações adicionais")).ToHaveValueAsync("Texto que deve continuar").ConfigureAwait(false);
        Assert.AreEqual("mesma-pagina", await Page.EvaluateAsync<string>("window.__marcador").ConfigureAwait(false));
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Campos atualizados" })).ToBeAttachedAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PriceMask_DigitaEstiloCaixaEletronico_IgnoraLetras_LimitaOTamanho_EOServidorLeOMesmoValor()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenNewAsync(Page).ConfigureAwait(false);
        ILocator price = Page.GetByLabel("Preço");

        (string typed, string expected)[] table =
        [
            ("1", "0,01"), ("12", "0,12"), ("123", "1,23"), ("1234567", "12.345,67"), ("6200000", "62.000,00"), ("9999999999", "99.999.999,99"), ("0062", "0,62")
        ];
        foreach ((string typed, string expected) in table)
        {
            await price.ClearAsync().ConfigureAwait(false);
            await price.PressSequentiallyAsync(typed).ConfigureAwait(false);
            await Expect(price).ToHaveValueAsync(expected).ConfigureAwait(false);
        }

        await price.ClearAsync().ConfigureAwait(false);
        await price.PressSequentiallyAsync("a1b2c3").ConfigureAwait(false);
        await Expect(price).ToHaveValueAsync("1,23").ConfigureAwait(false);
        await price.ClearAsync().ConfigureAwait(false);
        await price.PressSequentiallyAsync("123456789012345").ConfigureAwait(false);
        await Expect(price).ToHaveValueAsync("12.345.678,90").ConfigureAwait(false); // dígitos além do teto de 10 são ignorados
        await price.ClearAsync().ConfigureAwait(false);
        await Expect(price).ToHaveValueAsync("").ConfigureAwait(false);

        // Passagem: o que a máscara deixa no campo é gravado como esse valor em centavos e volta igual
        string title = Unique("Preço mascarado");
        await Page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        await price.PressSequentiallyAsync("6200000").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Rascunho salvo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Preço")).ToHaveValueAsync("62.000,00").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Contadores_MostramXdeN_DoGrupo_ContamQuebraDeLinhaComoUm_EAcompanhamATroca()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenNewAsync(Page).ConfigureAwait(false);
        ILocator titleCounter = Page.Locator("#contador-titulo");
        ILocator descriptionCounter = Page.Locator("#contador-descricao");

        await Expect(titleCounter).ToHaveTextAsync("0/120").ConfigureAwait(false);
        await Page.GetByLabel("Título").PressSequentiallyAsync("Casa").ConfigureAwait(false);
        await Expect(titleCounter).ToHaveTextAsync("4/120").ConfigureAwait(false);
        await Page.GetByLabel("Descrição").FillAsync("a\nb").ConfigureAwait(false);
        await Expect(descriptionCounter).ToHaveTextAsync("3/5000").ConfigureAwait(false);

        await ChooseCategoryAsync("Vagas de emprego").ConfigureAwait(false);
        await Expect(titleCounter).ToHaveTextAsync("4/90").ConfigureAwait(false);
        await Expect(descriptionCounter).ToHaveTextAsync("3/6000").ConfigureAwait(false);

        // O campo recusa digitar além do limite do grupo
        await Page.GetByLabel("Título").FillAsync(new string('x', 95)).ConfigureAwait(false);
        string typed = await Page.GetByLabel("Título").InputValueAsync().ConfigureAwait(false);
        Assert.IsLessThanOrEqualTo(90, typed.Length);
    }

    [TestMethod]
    public async Task US008S08_SemTitulo_MostraAMensagemJuntoAoCampo_ENaoCria_ComFocoNoCampo()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenNewAsync(Page).ConfigureAwait(false);
        await Page.GetByLabel("Descrição").FillAsync("Sem título").ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByText("Informe um título")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Título")).ToBeFocusedAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Título")).ToHaveAttributeAsync("aria-invalid", "true").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Descrição")).ToHaveValueAsync("Sem título").ConfigureAwait(false);
        StringAssert.DoesNotMatch(Page.Url, new Regex(@"/anuncios/\d+/editar"), "nada foi criado");
    }

    [TestMethod]
    public async Task US008S14_ServicoDeCepForaDoAr_MostraATentativa2de2_AbreOManual_ESalvaComSelo()
    {
        using FakeViaCep viaCep = new();
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenNewAsync(Page).ConfigureAwait(false);
        string title = Unique("Casa sem CEP");
        await Page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);

        await Page.GetByLabel("CEP").FillAsync("50000-000").ConfigureAwait(false);

        await Expect(Page.GetByText("Não foi possível buscar o CEP. Preencha Cidade e UF manualmente.").First).ToBeVisibleAsync().ConfigureAwait(false);
        Assert.AreEqual(2, viaCep.Hits("50000000"), "a primeira consulta e uma nova tentativa");
        await Expect(Page.Locator("#uf-manual")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.Locator("#uf-manual").SelectOptionAsync(new SelectOptionValue { Label = "SP" }).ConfigureAwait(false);
        ILocator city = Page.Locator("#cidade-manual");
        await Expect(city).ToBeVisibleAsync().ConfigureAwait(false);
        if (await city.EvaluateAsync<string>("e => e.tagName").ConfigureAwait(false) == "SELECT")
        {
            await Expect(city.Locator("option", new() { HasText = "Campinas" })).ToHaveCountAsync(1).ConfigureAwait(false);
            await city.SelectOptionAsync(new SelectOptionValue { Label = "Campinas" }).ConfigureAwait(false);
        }
        else
        {
            await city.FillAsync("Campinas").ConfigureAwait(false);
        }

        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Rascunho salvo" })).ToBeVisibleAsync().ConfigureAwait(false);

        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Page.Locator("#uf-manual")).ToHaveValueAsync("SP").ConfigureAwait(false);
        await Expect(Page.Locator("#cidade-manual")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Cep_Inexistente_MostraAMensagemSemAbrirOManual_ELimpaACidade()
    {
        using FakeViaCep viaCep = new();
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenNewAsync(Page).ConfigureAwait(false);

        await Page.GetByLabel("CEP").FillAsync("99999-999").ConfigureAwait(false);

        await Expect(Page.GetByText("CEP não encontrado. Confira os números.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("").ConfigureAwait(false);
        await Expect(Page.Locator("#uf-manual")).ToBeHiddenAsync().ConfigureAwait(false);
        Assert.AreEqual(1, viaCep.Hits("99999999"), "404 não repete");

        await Page.GetByLabel("CEP").FillAsync("1301").ConfigureAwait(false);
        await Page.GetByLabel("Título").ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Informe um CEP com 8 dígitos")).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SemJavaScript_ATrocaDeCategoriaPeloBotaoAtualizarCampos_ESalvarFuncionam()
    {
        await using IBrowserContext context = await Browser.NewContextAsync(new() { JavaScriptEnabled = false, IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(page).ConfigureAwait(false);
        await OpenNewAsync(page).ConfigureAwait(false);
        string title = Unique("Sem JS");

        await page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        await page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = Cars }).ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Atualizar campos" })).ToBeVisibleAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Atualizar campos" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByLabel("Quilometragem")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(page.GetByLabel("Título")).ToHaveValueAsync(title).ConfigureAwait(false);

        await page.GetByLabel("Marca").SelectOptionAsync(new SelectOptionValue { Label = "Honda" }).ConfigureAwait(false);
        await page.GetByLabel("Quilometragem").FillAsync("45000").ConfigureAwait(false);
        await page.GetByLabel("Preço").FillAsync("62000").ConfigureAwait(false);
        await page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);

        await Expect(page.GetByText("Rascunho salvo")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(page.GetByLabel("Título")).ToHaveValueAsync(title).ConfigureAwait(false);
        await Expect(page.GetByLabel("Preço")).ToHaveValueAsync("62.000,00").ConfigureAwait(false);
        await Expect(page.GetByLabel("Quilometragem")).ToHaveValueAsync("45000").ConfigureAwait(false);
        await Expect(page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        await Expect(page.GetByLabel("Marca")).ToHaveValueAsync("1").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_FormularioNovo_ComCategoria_ComErros_EEmTelaDe320px()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenNewAsync(Page).ConfigureAwait(false);
        await ChooseCategoryAsync(Cars).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Quilometragem")).ToBeVisibleAsync().ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };

        AxeResult blank = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, blank.Violations.Length, "formulário de carros: " + string.Join("; ", blank.Violations.Select(Describe)));

        await ChooseCategoryAsync("Vagas de emprego").ConfigureAwait(false);
        await Expect(Page.GetByText("Vagas de emprego não têm fotos")).ToBeVisibleAsync().ConfigureAwait(false);
        AxeResult jobs = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, jobs.Violations.Length, "formulário de vagas: " + string.Join("; ", jobs.Violations.Select(Describe)));

        // Com erros: sem título, preço fora do formato (digitado sem a máscara) e campo numérico com texto
        await ChooseCategoryAsync(Cars).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Quilometragem")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByLabel("Quilometragem").FillAsync("muito").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Informe um título")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Informe apenas números")).ToBeVisibleAsync().ConfigureAwait(false);
        AxeResult errors = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, errors.Violations.Length, "formulário com erros: " + string.Join("; ", errors.Violations.Select(Describe)));

        await Page.SetViewportSizeAsync(320, 800).ConfigureAwait(false);
        bool overflows = await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false);
        string culprits = overflows
            ? await Page.EvaluateAsync<string>("[...document.querySelectorAll('body *')].filter(e => e.getBoundingClientRect().right > document.documentElement.clientWidth + 1).slice(0, 5).map(e => e.tagName + '#' + e.id + '.' + e.className + ' ' + Math.round(e.getBoundingClientRect().left) + '-' + Math.round(e.getBoundingClientRect().right) + ' ' + e.textContent.slice(0, 40)).join(' | ')").ConfigureAwait(false)
            : string.Empty;
        Assert.IsFalse(overflows, "o formulário não rola na horizontal em 320 px: " + culprits);
    }
}

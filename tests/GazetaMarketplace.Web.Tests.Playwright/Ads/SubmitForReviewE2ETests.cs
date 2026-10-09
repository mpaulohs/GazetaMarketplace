using System;
using System.Collections.Generic;
using System.IO;
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
/// Enviar o anúncio para revisão (US-009) no navegador (roda no /test): a jornada completa, a lista de pendências com links que levam o foco ao campo, o clique duplo
/// e o mesmo caminho sem JavaScript. Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD (conta de Administrador) e GAZETA_E2E_VIACEP_PORT. O reenvio de um
/// anúncio rejeitado (S04) não tem E2E: o Administrador só rejeita pela fila de revisão, que chega na tarefa 4.1 (o caminho está nos testes de unidade).
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class SubmitForReviewE2ETests : SitePage
#pragma warning restore CA1515
{
    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static string Photo => Path.Combine(AppContext.BaseDirectory, "Photos", "Fixtures", "foto-1.jpg");

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    /// <summary>ViaCEP de mentira: só o CEP 13015100 (Campinas/SP) existe.</summary>
    private sealed class FakeViaCep : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _stop = new();

        public FakeViaCep()
        {
            int port = int.Parse(RequiresVariablesAttribute.Value("GAZETA_E2E_VIACEP_PORT"), System.Globalization.CultureInfo.InvariantCulture);
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _ = Task.Run(LoopAsync);
        }

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

                bool known = Regex.IsMatch(context.Request.Url!.AbsolutePath, @"^/ws/13015100/json/$");
                context.Response.StatusCode = known ? 200 : 500;
                context.Response.ContentType = "application/json";
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(known ? """{"cep":"13015-100","localidade":"Campinas","uf":"SP","ibge":"3509502"}""" : "falhou")).ConfigureAwait(false);
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

    /// <summary>Cria e salva um rascunho de "Livros e revistas"; <paramref name="complete"/> preenche tudo o que o envio exige, menos a foto.</summary>
    private async Task<string> CreateDraftAsync(IPage page, string title, bool complete)
    {
        await page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        await page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        if (complete)
        {
            await page.GetByLabel("Descrição").FillAsync("Edição 2020, sem anotações").ConfigureAwait(false);
            await page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
            await page.GetByLabel("Condição").WaitForAsync().ConfigureAwait(false);
            await page.GetByLabel("Condição").SelectOptionAsync(new SelectOptionValue { Index = 1 }).ConfigureAwait(false);
            await page.GetByLabel("Preço").FillAsync("5000").ConfigureAwait(false);
            await page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
            await Expect(page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);
        await page.GetByText("Rascunho salvo").WaitForAsync().ConfigureAwait(false);
        return page.Url;
    }

    private static async Task AddPhotoAsync(IPage page)
    {
        await page.GetByLabel("Escolher fotos").SetInputFilesAsync(Photo).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (1 de" }).WaitForAsync().ConfigureAwait(false);
    }

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

    [TestMethod]
    public async Task US009S01_EnviarUmRascunhoCompleto_ConfirmaEVeAMensagem_EOAnuncioFicaSomenteLeitura()
    {
        using FakeViaCep viaCep = new();
        List<string> failures = WatchFailures(Page);
        await SignInAsync(Page).ConfigureAwait(false);
        string title = Unique("Livro enviado");
        string editUrl = await CreateDraftAsync(Page, title, complete: true).ConfigureAwait(false);
        await AddPhotoAsync(Page).ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Enviar para revisão?" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText(title)).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByText("Anúncio enviado para revisão")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GotoAsync(editUrl).ConfigureAwait(false);
        // A conta do E2E é de Administrador, que ainda edita um anúncio Em revisão; o que muda é a situação e que o envio não é mais oferecido
        await Expect(Page.GetByText("Situação:").Locator("strong")).ToHaveTextAsync("Em revisão").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" })).ToHaveCountAsync(0).ConfigureAwait(false);
        Assert.IsEmpty(failures, "respostas de erro do site na jornada: " + string.Join("; ", failures));
    }

    [TestMethod]
    public async Task US009S02eS03_RascunhoIncompleto_MostraAListaDePendencias_OsLinksLevamOFocoAoCampo()
    {
        using FakeViaCep viaCep = new();
        await SignInAsync(Page).ConfigureAwait(false);
        string editUrl = await CreateDraftAsync(Page, Unique("Só o título"), complete: false).ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);

        ILocator list = Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "para enviar este anúncio para revisão" });
        await Expect(list).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(list).ToBeFocusedAsync().ConfigureAwait(false);
        foreach (string pending in new[] { "Escolha uma categoria", "Informe a descrição", "Informe o CEP" })
        {
            await Expect(list.GetByRole(AriaRole.Link, new() { Name = pending })).ToBeVisibleAsync().ConfigureAwait(false);
        }

        // Cada link leva o foco ao campo
        await list.GetByRole(AriaRole.Link, new() { Name = "Informe o CEP" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("CEP")).ToBeFocusedAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "para enviar este anúncio" }).GetByRole(AriaRole.Link, new() { Name = "Informe a descrição" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Descrição")).ToBeFocusedAsync().ConfigureAwait(false);

        // Faltou só a característica da categoria (S03): escolhe a categoria, preenche o resto e deixa a Condição em branco
        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
        await Page.GetByLabel("Condição").WaitForAsync().ConfigureAwait(false);
        await Page.GetByLabel("Descrição").FillAsync("Edição 2020").ConfigureAwait(false);
        await Page.GetByLabel("Preço").FillAsync("5000").ConfigureAwait(false);
        await Page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        await AddPhotoAsync(Page).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);

        ILocator condition = Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Falta 1 item" });
        await Expect(condition.GetByRole(AriaRole.Link, new() { Name = "Informe a condição" })).ToBeVisibleAsync().ConfigureAwait(false);
        await condition.GetByRole(AriaRole.Link, new() { Name = "Informe a condição" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Condição")).ToBeFocusedAsync().ConfigureAwait(false);
        await Page.GotoAsync(editUrl).ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:").Locator("strong")).ToHaveTextAsync("Rascunho").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US009S05_CliqueDuploEmEnviar_AnuncioEnviadoUmaVez_SemErro()
    {
        using FakeViaCep viaCep = new();
        List<string> failures = WatchFailures(Page);
        await SignInAsync(Page).ConfigureAwait(false);
        string editUrl = await CreateDraftAsync(Page, Unique("Clique duplo"), complete: true).ConfigureAwait(false);
        await AddPhotoAsync(Page).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Enviar para revisão?" })).ToBeVisibleAsync().ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync(new() { ClickCount = 2, Delay = 10 }).ConfigureAwait(false);

        await Expect(Page.GetByText("Anúncio enviado para revisão")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GotoAsync(editUrl).ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:").Locator("strong")).ToHaveTextAsync("Em revisão").ConfigureAwait(false);
        Assert.IsEmpty(failures, "o clique duplo não gera nenhuma resposta de erro: " + string.Join("; ", failures));
    }

    [TestMethod]
    public async Task SemJavaScript_SalvaConfereConfirmaEEnvia_PorFormulariosComuns()
    {
        using FakeViaCep viaCep = new();
        await using IBrowserContext context = await Browser.NewContextAsync(new() { JavaScriptEnabled = false, IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(page).ConfigureAwait(false);
        string editUrl = await CreateDraftAsync(page, Unique("Sem JS envio"), complete: false).ConfigureAwait(false);

        // Sem foto, sem descrição e sem categoria: a lista aparece (a página recarrega)
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Adicione ao menos 1 foto" })).ToBeVisibleAsync().ConfigureAwait(false);

        // Completa o rascunho em páginas comuns e envia
        await page.GetByLabel("Descrição").FillAsync("Edição 2020").ConfigureAwait(false);
        await page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Atualizar campos" }).ClickAsync().ConfigureAwait(false);
        await page.GetByLabel("Condição").SelectOptionAsync(new SelectOptionValue { Index = 1 }).ConfigureAwait(false);
        await page.GetByLabel("Preço").FillAsync("50").ConfigureAwait(false);
        await page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);
        await page.GetByText("Rascunho salvo").WaitForAsync().ConfigureAwait(false);
        await page.GetByLabel("Escolher fotos").SetInputFilesAsync(Photo).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByText("Foto adicionada.")).ToBeVisibleAsync().ConfigureAwait(false);

        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Enviar para revisão?" })).ToBeVisibleAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);

        await Expect(page.GetByText("Anúncio enviado para revisão")).ToBeVisibleAsync().ConfigureAwait(false);
        await page.GotoAsync(editUrl).ConfigureAwait(false);
        await Expect(page.GetByText("Situação:").Locator("strong")).ToHaveTextAsync("Em revisão").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_ListaDePendenciasEConfirmacao_SemViolacoes_ENaoRolaNaHorizontalEm320px()
    {
        using FakeViaCep viaCep = new();
        await SignInAsync(Page).ConfigureAwait(false);
        string editUrl = await CreateDraftAsync(Page, Unique("Acessível"), complete: false).ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };

        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "para enviar este anúncio" })).ToBeVisibleAsync().ConfigureAwait(false);
        AxeResult pending = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, pending.Violations.Length, "lista de pendências: " + string.Join("; ", pending.Violations.Select(Describe)));
        await Page.SetViewportSizeAsync(320, 800).ConfigureAwait(false);
        Assert.IsFalse(await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false), "a lista não rola na horizontal em 320 px");

        await Page.SetViewportSizeAsync(1280, 800).ConfigureAwait(false);
        await Page.GotoAsync(editUrl).ConfigureAwait(false);
        await Page.GetByLabel("Descrição").FillAsync("Edição 2020").ConfigureAwait(false);
        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
        await Page.GetByLabel("Condição").WaitForAsync().ConfigureAwait(false);
        await Page.GetByLabel("Condição").SelectOptionAsync(new SelectOptionValue { Index = 1 }).ConfigureAwait(false);
        await Page.GetByLabel("Preço").FillAsync("5000").ConfigureAwait(false);
        await Page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);
        await Page.GetByText("Rascunho salvo").WaitForAsync().ConfigureAwait(false);
        await AddPhotoAsync(Page).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Enviar para revisão?" })).ToBeVisibleAsync().ConfigureAwait(false);
        // O mouse sai de cima do botão e a transição de cor do Bootstrap termina antes da medição
        await Page.Mouse.MoveAsync(0, 0).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Cancelar" })).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)").ConfigureAwait(false);
        AxeResult confirmation = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, confirmation.Violations.Length, "confirmação: " + string.Join("; ", confirmation.Violations.Select(Describe)));
        await Page.SetViewportSizeAsync(320, 800).ConfigureAwait(false);
        Assert.IsFalse(await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false), "a confirmação não rola na horizontal em 320 px");
    }
}

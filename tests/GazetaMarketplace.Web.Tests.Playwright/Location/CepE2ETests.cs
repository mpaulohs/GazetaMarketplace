using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Location;

/// <summary>
/// <c>cep.js</c> falando com o endpoint de verdade (roda no /test): o módulo, o servidor, o cache e o contrato 503 × 404 (ADR-007). Além das variáveis dos
/// outros E2E, usa GAZETA_E2E_VIACEP_PORT: porta de um ViaCEP de mentira que o próprio teste abre e para onde o site (iniciado com
/// <c>ViaCep__BaseUrl=http://localhost:PORTA/ws/</c>) manda as consultas. A tela do formulário é da tarefa 3.3; aqui o módulo é chamado direto, de uma página do painel.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class CepE2ETests : SitePage
#pragma warning restore CA1515
{
    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    /// <summary>ViaCEP de mentira: cada CEP tem o seu comportamento e as chamadas são contadas.</summary>
    private sealed class FakeViaCep : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly ConcurrentDictionary<string, int> _hits = new();

        public FakeViaCep(int port)
        {
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
                int hit = _hits.AddOrUpdate(cep, 1, (_, n) => n + 1);

                (int status, string body) = cep switch
                {
                    // Rua e bairro vêm na resposta externa e não podem chegar ao navegador
                    "13015300" => (200, """{"cep":"13015-300","logradouro":"Rua Barão de Jaguara","bairro":"Centro","localidade":"Campinas","uf":"SP","ibge":"3509502"}"""),
                    "99999999" => (200, """{"erro":true}"""),
                    "50000000" => (500, "falhou"),
                    "60000000" => hit == 1 ? (500, "falhou") : (200, """{"localidade":"Valinhos","uf":"SP","ibge":"3556206"}"""),
                    _ => (404, "?")
                };
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                await context.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
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

    private static async Task<JsonElement> RunAsync(IPage page, string call)
    {
        // Chama o módulo real, servido pelo site, de uma página do painel. As tentativas anunciadas à tela voltam junto com o resultado
        string script = "async () => { const m = await import('/js/modules/cep.js'); const tentativas = []; const r = await "
            + call.Replace("$ATTEMPTS", "{ aoTentar: (t, n) => tentativas.push([t, n]) }")
            + "; return { r, tentativas }; }";
        return await page.EvaluateAsync<JsonElement>(script).ConfigureAwait(false);
    }

    private static string Status(JsonElement result) => result.GetProperty("r").GetProperty("status").GetString();

    [TestMethod]
    public async Task CepJs_Encontrado_Cache_Incompleto_NaoEncontrado_ERepeticaoSoEm503()
    {
        int port = int.Parse(RequiresVariablesAttribute.Value("GAZETA_E2E_VIACEP_PORT"), System.Globalization.CultureInfo.InvariantCulture);
        using FakeViaCep viaCep = new(port);
        await SignInAsync(Page).ConfigureAwait(false);

        // 200: a primeira vem do ViaCEP, a segunda do cache do banco (uma consulta externa só). Pontuação do CEP digitado é ignorada
        // O CEP 13015-300 é só deste teste: os outros E2E cadastram anúncios com 13015-100 e deixam essa entrada no cache de CEP do banco; com ele, este teste não depende da ordem de execução
        JsonElement found = await RunAsync(Page, "m.consultarCep('13015-300', $ATTEMPTS)").ConfigureAwait(false);
        JsonElement cached = await RunAsync(Page, "m.consultarCep('13015300', $ATTEMPTS)").ConfigureAwait(false);
        Assert.AreEqual("encontrado", Status(found));
        Assert.AreEqual("Campinas", found.GetProperty("r").GetProperty("cidade").GetString());
        Assert.AreEqual("SP", found.GetProperty("r").GetProperty("uf").GetString());
        Assert.AreEqual("viacep", found.GetProperty("r").GetProperty("origem").GetString());
        Assert.AreEqual("cache", cached.GetProperty("r").GetProperty("origem").GetString());
        Assert.AreEqual(1, viaCep.Hits("13015300"), "o segundo pedido não chegou ao ViaCEP");
        StringAssert.DoesNotMatch(found.GetRawText(), new Regex("Jaguara|Centro|logradouro|bairro"), "rua e bairro não chegam ao navegador");

        // S23: menos de 8 dígitos não chama o servidor
        JsonElement incomplete = await RunAsync(Page, "m.consultarCep('1301', $ATTEMPTS)").ConfigureAwait(false);
        Assert.AreEqual("incompleto", Status(incomplete));
        Assert.AreEqual(0, incomplete.GetProperty("tentativas").GetArrayLength());

        // S24: 404 não é falha do serviço: nenhuma nova tentativa, e a tela NÃO abre o manual
        JsonElement missing = await RunAsync(Page, "m.consultarCep('99999999', $ATTEMPTS)").ConfigureAwait(false);
        Assert.AreEqual("naoEncontrado", Status(missing));
        Assert.AreEqual("CEP não encontrado. Confira os números.", missing.GetProperty("r").GetProperty("mensagem").GetString());
        Assert.AreEqual(0, missing.GetProperty("tentativas").GetArrayLength(), "404 não repete");
        Assert.AreEqual(1, viaCep.Hits("99999999"));
    }

    [TestMethod]
    public async Task CepJs_503_RepeteUmaVez_AnunciaATentativa2de2_EDepoisAbreOManual()
    {
        int port = int.Parse(RequiresVariablesAttribute.Value("GAZETA_E2E_VIACEP_PORT"), System.Globalization.CultureInfo.InvariantCulture);
        using FakeViaCep viaCep = new(port);
        await SignInAsync(Page).ConfigureAwait(false);

        // Cai duas vezes: "Buscando… (tentativa 2 de 2)" e depois o preenchimento manual (S14)
        JsonElement down = await RunAsync(Page, "m.consultarCep('50000000', $ATTEMPTS)").ConfigureAwait(false);
        Assert.AreEqual("indisponivel", Status(down));
        Assert.AreEqual(1, down.GetProperty("tentativas").GetArrayLength());
        Assert.AreEqual(2, down.GetProperty("tentativas")[0][0].GetInt32());
        Assert.AreEqual(2, down.GetProperty("tentativas")[0][1].GetInt32());
        Assert.AreEqual(2, viaCep.Hits("50000000"), "exatamente duas chamadas: a primeira e uma nova tentativa");

        // Cai na primeira e responde na segunda: a tentativa 2 resolve e o manual não abre
        JsonElement flaky = await RunAsync(Page, "m.consultarCep('60000000', $ATTEMPTS)").ConfigureAwait(false);
        Assert.AreEqual("encontrado", Status(flaky));
        Assert.AreEqual("Valinhos", flaky.GetProperty("r").GetProperty("cidade").GetString());
        Assert.AreEqual(1, flaky.GetProperty("tentativas").GetArrayLength());
        Assert.AreEqual(2, viaCep.Hits("60000000"));
    }

    [TestMethod]
    public async Task Endpoints_SemLogin_Respondem401EmJson_ECidadesComUfInvalida400()
    {
        int port = int.Parse(RequiresVariablesAttribute.Value("GAZETA_E2E_VIACEP_PORT"), System.Globalization.CultureInfo.InvariantCulture);
        using FakeViaCep viaCep = new(port);

        // Sem entrar: o cookie não existe, a resposta é 401 em JSON e nada vai ao ViaCEP
        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        JsonElement anonymous = await Page.EvaluateAsync<JsonElement>(
            "async () => { const r = await fetch('/api/v1/cep/13015100'); const c = await fetch('/api/v1/cities?uf=SP'); return { cep: r.status, cities: c.status, tipo: r.headers.get('content-type') }; }").ConfigureAwait(false);
        Assert.AreEqual(401, anonymous.GetProperty("cep").GetInt32());
        Assert.AreEqual(401, anonymous.GetProperty("cities").GetInt32());
        StringAssert.Contains(anonymous.GetProperty("tipo").GetString(), "problem+json");

        await SignInAsync(Page).ConfigureAwait(false);
        JsonElement cities = await RunAsync(Page, "m.listarCidades('SP')").ConfigureAwait(false);
        Assert.AreEqual(JsonValueKind.Array, cities.GetProperty("r").ValueKind);
        JsonElement invalid = await Page.EvaluateAsync<JsonElement>(
            "async () => { const m = await import('/js/modules/cep.js'); try { await m.listarCidades('XX'); return { status: 0 }; } catch (e) { return { status: e.status, code: e.code }; } }").ConfigureAwait(false);
        Assert.AreEqual(400, invalid.GetProperty("status").GetInt32());
        Assert.AreEqual("VALIDATION_ERROR", invalid.GetProperty("code").GetString());
        Assert.AreEqual(0, viaCep.Hits("13015100"), "sem login nada foi consultado");
    }
}

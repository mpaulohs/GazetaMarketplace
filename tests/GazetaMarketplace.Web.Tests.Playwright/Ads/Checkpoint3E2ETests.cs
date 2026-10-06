using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Ads;

/// <summary>
/// Checkpoint 3 (Anúncios completos), no site publicado: a jornada rascunho → fotos → envio para revisão para Carros, Serviços e Vagas, e o que o site entrega das fotos
/// (HEIC convertido, nenhum GPS nas versões entregues, os originais sem rota). Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class Checkpoint3E2ETests : SitePage
#pragma warning restore CA1515
{
    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Photos", "Fixtures", name);

    private async Task SignInAsync()
    {
        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await Page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await Page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
    }

    private List<string> WatchFailures()
    {
        List<string> failures = [];
        string origin = new Uri(Url("/")).GetLeftPart(UriPartial.Authority);
        Page.Response += (_, response) =>
        {
            if (response.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase) && response.Status >= 400)
            {
                failures.Add($"{response.Status} {response.Url}");
            }
        };
        return failures;
    }

    private async Task OpenNewAsync(string title)
    {
        await Page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        // Logo depois de o site subir, os scripts da página chegam tarde e refazem os campos; só age quando a rede assentou
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await Page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
    }

    private async Task FillCepAsync()
    {
        await Page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
    }

    private async Task<string> SaveAsync()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Rascunho salvo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        return Page.Url;
    }

    /// <summary>O JavaScript da página já rodou: é ele que marca o campo de fotos como "vários arquivos". Sem esperar, o teste podia agir na página ainda sem script.</summary>
    private Task WaitForScriptAsync() => Page.WaitForFunctionAsync("() => document.getElementById('arquivo-foto')?.multiple === true");

    private async Task UploadAsync(params FilePayload[] files)
    {
        await WaitForScriptAsync().ConfigureAwait(false);
        await Page.GetByLabel("Escolher fotos").SetInputFilesAsync(files).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = $"Fotos ({files.Length} de" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    private static FilePayload Jpeg(string name = "foto-1.jpg") => new() { Name = name, MimeType = "image/jpeg", Buffer = File.ReadAllBytes(Fixture(name)) };

    /// <summary>Envia pelo botão, passa pela confirmação e volta a abrir o anúncio para conferir a situação gravada no servidor.</summary>
    private async Task SendForReviewAsync(string editUrl)
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Enviar para revisão?" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Anúncio enviado para revisão")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GotoAsync(editUrl).ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:").Locator("strong")).ToHaveTextAsync("Em revisão").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Carros_RascunhoComFoto_SemFotoNaoEnvia_ComFotoVaiParaRevisao()
    {
        using FakeViaCep viaCep = new();
        List<string> failures = WatchFailures();
        await SignInAsync().ConfigureAwait(false);
        await OpenNewAsync(Unique("Honda Civic 2018")).ConfigureAwait(false);
        await Page.GetByLabel("Descrição").FillAsync("Único dono, revisões em dia").ConfigureAwait(false);
        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Carros, vans e utilitários" }).ConfigureAwait(false);
        await Page.GetByLabel("Preço").FillAsync("62000").ConfigureAwait(false);
        await FillCepAsync().ConfigureAwait(false);
        await Page.GetByLabel("Marca").SelectOptionAsync(new SelectOptionValue { Label = "Honda" }).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Modelo")).ToBeEnabledAsync().ConfigureAwait(false);
        await Page.GetByLabel("Modelo").SelectOptionAsync(new SelectOptionValue { Label = "Civic" }).ConfigureAwait(false);
        await Expect(Page.GetByLabel(new Regex(@"^Ano\b"))).ToBeEnabledAsync().ConfigureAwait(false);
        await Page.GetByLabel(new Regex(@"^Ano\b")).SelectOptionAsync(new SelectOptionValue { Label = "2018" }).ConfigureAwait(false);
        await Expect(Page.GetByLabel(new Regex(@"^Versão\b"))).ToBeEnabledAsync().ConfigureAwait(false);
        await Page.GetByLabel(new Regex(@"^Versão\b")).SelectOptionAsync(new SelectOptionValue { Index = 1 }).ConfigureAwait(false);
        await Page.GetByLabel("Quilometragem").FillAsync("45000").ConfigureAwait(false);
        string editUrl = await SaveAsync().ConfigureAwait(false);

        // Sem foto o envio mostra só a pendência da foto e a situação continua Rascunho
        await WaitForScriptAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        ILocator pending = Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Falta 1 item" });
        await Expect(pending.GetByRole(AriaRole.Link, new() { Name = "Adicione ao menos 1 foto" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GotoAsync(editUrl).ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:").Locator("strong")).ToHaveTextAsync("Rascunho").ConfigureAwait(false);

        await UploadAsync(Jpeg()).ConfigureAwait(false);
        await SendForReviewAsync(editUrl).ConfigureAwait(false);
        Assert.IsEmpty(failures, "respostas de erro do site na jornada: " + string.Join("; ", failures));
    }

    [TestMethod]
    public async Task Servicos_SemPreco_ComDuasFotos_VaiParaRevisao()
    {
        using FakeViaCep viaCep = new();
        List<string> failures = WatchFailures();
        await SignInAsync().ConfigureAwait(false);
        await OpenNewAsync(Unique("Diarista")).ConfigureAwait(false);
        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Serviços" }).ConfigureAwait(false);
        await Page.GetByLabel("Informações adicionais").FillAsync("Atendo a região toda, com referências").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Preço")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Page.GetByLabel("Tipo").SelectOptionAsync(new SelectOptionValue { Index = 1 }).ConfigureAwait(false);
        await FillCepAsync().ConfigureAwait(false);
        string editUrl = await SaveAsync().ConfigureAwait(false);
        await UploadAsync(Jpeg("foto-1.jpg"), Jpeg("foto-2.jpg")).ConfigureAwait(false);

        await SendForReviewAsync(editUrl).ConfigureAwait(false);
        Assert.IsEmpty(failures, "respostas de erro do site na jornada: " + string.Join("; ", failures));
    }

    [TestMethod]
    public async Task Vagas_SemFotos_ComSalario_VaiParaRevisaoSemExigirFoto()
    {
        using FakeViaCep viaCep = new();
        List<string> failures = WatchFailures();
        await SignInAsync().ConfigureAwait(false);
        await OpenNewAsync(Unique("Pizzaiolo")).ConfigureAwait(false);
        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Vagas de emprego" }).ConfigureAwait(false);
        await Page.GetByLabel("Informações adicionais").FillAsync("Período integral, com experiência").ConfigureAwait(false);
        await Expect(Page.GetByText("Vagas de emprego não têm fotos")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByLabel("Salário").FillAsync("280000").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Salário")).ToHaveValueAsync("2.800,00").ConfigureAwait(false);
        await FillCepAsync().ConfigureAwait(false);
        string editUrl = await SaveAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Escolher fotos")).ToHaveCountAsync(0).ConfigureAwait(false);

        await SendForReviewAsync(editUrl).ConfigureAwait(false);
        Assert.IsEmpty(failures, "respostas de erro do site na jornada: " + string.Join("; ", failures));
    }

    [TestMethod]
    public async Task TrocaDeCategoria_PrecoDigitadoEnquantoOServidorResponde_NaoSePerde_ESeguePodendoDigitar()
    {
        await SignInAsync().ConfigureAwait(false);
        await OpenNewAsync(Unique("Digitando durante a troca")).ConfigureAwait(false);
        // O servidor demora a devolver os campos do grupo (como no primeiro pedido depois de o site subir)
        await Page.RouteAsync("**/painel/anuncios/campos?*", async route =>
        {
            await Task.Delay(1500).ConfigureAwait(false);
            await route.ContinueAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Carros, vans e utilitários" }).ConfigureAwait(false);
        ILocator price = Page.GetByLabel("Preço");
        await price.PressSequentiallyAsync("6200000").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Campos atualizados" })).ToBeAttachedAsync().ConfigureAwait(false);

        await Expect(Page.GetByLabel("Marca")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(price).ToHaveValueAsync("62.000,00").ConfigureAwait(false);
        await Expect(price).ToBeFocusedAsync().ConfigureAwait(false);
        await price.PressSequentiallyAsync("1").ConfigureAwait(false);
        await Expect(price).ToHaveValueAsync("620.000,01").ConfigureAwait(false);
    }

    // R-04: com a sessão vencida o servidor redireciona o fetch dos campos para a tela de entrada; ela nunca pode ser injetada no formulário
    [TestMethod]
    public async Task TrocaDeCategoria_ComASessaoVencida_MostraOAvisoEMantemOFormulario_SemInjetarATelaDeEntrada()
    {
        await SignInAsync().ConfigureAwait(false);
        string title = Unique("Sessão vencida");
        await OpenNewAsync(title).ConfigureAwait(false);
        await Page.Context.ClearCookiesAsync().ConfigureAwait(false);

        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Carros, vans e utilitários" }).ConfigureAwait(false);

        ILocator notice = Page.Locator("[data-session-expired]");
        await Expect(notice).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(notice).ToContainTextAsync("Sua sessão expirou").ConfigureAwait(false);
        await Expect(notice.GetByRole(AriaRole.Link, new() { Name = "entre de novo em outra aba" })).ToHaveAttributeAsync("target", "_blank").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Título")).ToHaveValueAsync(title).ConfigureAwait(false);
        await Expect(Page.Locator("#anuncio-form input[type=password]")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.Locator("#anuncio-form").GetByText("Esqueci minha senha")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Marca")).ToHaveCountAsync(0).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fotos_JpegComGpsEHeic_ChegamComoWebpSemGps_EOsOriginaisNaoTemRota()
    {
        using FakeViaCep viaCep = new();
        await SignInAsync().ConfigureAwait(false);
        await OpenNewAsync(Unique("Fotos do checkpoint")).ConfigureAwait(false);
        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
        await Page.GetByLabel("Condição").WaitForAsync().ConfigureAwait(false);
        await SaveAsync().ConfigureAwait(false);
        byte[] withGps = JpegWithGps(File.ReadAllBytes(Fixture("foto-1.jpg")));
        Assert.IsTrue(IndexOf(withGps, GpsLatitude) >= 0, "o JPEG enviado tem mesmo latitude no EXIF");

        await UploadAsync(
            new FilePayload { Name = "com-gps.jpg", MimeType = "image/jpeg", Buffer = withGps },
            new FilePayload { Name = "iphone.heic", MimeType = "image/heic", Buffer = File.ReadAllBytes(Fixture("sample.heic")) }).ConfigureAwait(false);

        IReadOnlyList<string> sources = await Page.Locator("[data-photo-id] img[src^='/fotos/']").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('src'))").ConfigureAwait(false);
        Assert.HasCount(2, sources, "duas miniaturas na galeria");
        foreach (string thumb in sources)
        {
            foreach (string size in new[] { "480", "1600" })
            {
                IAPIResponse response = await Page.Context.APIRequest.GetAsync(Url(thumb.Replace("-480.webp", $"-{size}.webp", StringComparison.Ordinal))).ConfigureAwait(false);
                Assert.AreEqual(200, response.Status, $"{thumb} ({size})");
                Assert.AreEqual("image/webp", response.Headers["content-type"]);
                byte[] body = await response.BodyAsync().ConfigureAwait(false);
                Assert.AreEqual("RIFF", Encoding.ASCII.GetString(body, 0, 4));
                Assert.AreEqual("WEBP", Encoding.ASCII.GetString(body, 8, 4), "tanto o JPEG quanto o HEIC viram WebP");
                CollectionAssert.DoesNotContain(WebpChunks(body), "EXIF", "nenhum bloco EXIF na versão entregue");
                CollectionAssert.DoesNotContain(WebpChunks(body), "XMP ");
                Assert.AreEqual(-1, IndexOf(body, GpsLatitude), "a latitude do GPS não aparece nos bytes entregues");
            }
        }

        // Os originais (onde o GPS fica guardado) nunca saem: nenhuma rota entrega _originals/
        Match ids = Regex.Match(sources[0], @"^/fotos/(\d+)/(\d+)-480\.webp$");
        Assert.IsTrue(ids.Success, sources[0]);
        string adId = ids.Groups[1].Value;
        string photoId = ids.Groups[2].Value;
        foreach (string path in new[]
        {
            $"/fotos/{adId}/_originals/qualquer.jpg", $"/fotos/_originals/{adId}/qualquer.jpg", "/_originals/2026-10/qualquer.jpg", "/fotos/_originals/2026-10/qualquer.jpg",
            $"/fotos/{adId}/{photoId}-original.webp", $"/fotos/{adId}/{photoId}-original.jpg", $"/fotos/{adId}/{photoId}.jpg", $"/fotos/{adId}/..%2F_originals%2Fqualquer.jpg"
        })
        {
            IAPIResponse response = await Page.Context.APIRequest.GetAsync(Url(path)).ConfigureAwait(false);
            Assert.AreEqual(404, response.Status, $"{path} não pode ser entregue");
        }

        // Anúncio ainda em rascunho: quem não entrou não vê nem as versões entregues (404 igual ao de "não existe")
        IAPIRequestContext anonymous = await Playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IAPIResponse hidden = await anonymous.GetAsync(Url(sources[0])).ConfigureAwait(false);
        Assert.AreEqual(404, hidden.Status, "rascunho não é público");
        await anonymous.DisposeAsync().ConfigureAwait(false);
    }

    // --- JPEG com GPS montado à mão: APP1/Exif, TIFF big-endian, IFD0 com o ponteiro GPS e latitude 23°32'10" S / longitude 46°38'5" W ---

    private static readonly byte[] GpsLatitude = [0, 0, 0, 23, 0, 0, 0, 1, 0, 0, 0, 32, 0, 0, 0, 1, 0, 0, 0, 10, 0, 0, 0, 1];

    private static int IndexOf(byte[] data, byte[] pattern) => data.AsSpan().IndexOf(pattern);

    private static void Put16(List<byte> bytes, int value) { bytes.Add((byte)(value >> 8)); bytes.Add((byte)value); }

    private static void Put32(List<byte> bytes, int value) { Put16(bytes, value >> 16); Put16(bytes, value & 0xFFFF); }

    private static void Entry(List<byte> bytes, int tag, int type, int count, int value)
    {
        Put16(bytes, tag);
        Put16(bytes, type);
        Put32(bytes, count);
        Put32(bytes, value);
    }

    private static byte[] JpegWithGps(byte[] jpeg)
    {
        List<byte> tiff = [(byte)'M', (byte)'M'];
        Put16(tiff, 42);
        Put32(tiff, 8);
        // IFD0 (offset 8): 1 entrada, o ponteiro para o GPS IFD (offset 26)
        Put16(tiff, 1);
        Entry(tiff, 0x8825, 4, 1, 26);
        Put32(tiff, 0);
        // GPS IFD (offset 26): 4 entradas; os valores de 3 racionais ficam depois (80 e 104)
        Put16(tiff, 4);
        Entry(tiff, 1, 2, 2, 'S' << 24);
        Entry(tiff, 2, 5, 3, 80);
        Entry(tiff, 3, 2, 2, 'W' << 24);
        Entry(tiff, 4, 5, 3, 104);
        Put32(tiff, 0);
        foreach (int number in new[] { 23, 1, 32, 1, 10, 1, 46, 1, 38, 1, 5, 1 })
        {
            Put32(tiff, number);
        }

        List<byte> app1 = [0xFF, 0xE1];
        Put16(app1, 2 + 6 + tiff.Count);
        app1.AddRange(Encoding.ASCII.GetBytes("Exif\0\0"));
        app1.AddRange(tiff);
        // O JPEG original começa em FFD8; o EXIF entra logo depois
        return [.. jpeg.Take(2), .. app1, .. jpeg.Skip(2)];
    }

    private static List<string> WebpChunks(byte[] webp)
    {
        List<string> chunks = [];
        int position = 12;
        while (position + 8 <= webp.Length)
        {
            chunks.Add(Encoding.ASCII.GetString(webp, position, 4));
            int size = BitConverter.ToInt32(webp, position + 4);
            position += 8 + size + (size & 1);
        }

        return chunks;
    }
}

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Detail.AdDetailTests;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Contact;

/// <summary>O contato do intermediário na página do anúncio (US-004, S01 a S05; tarefa 5.3): número escrito, "Ligar" e "Chamar no WhatsApp" sem login, com a mensagem pronta.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ContactTests
#pragma warning restore CA1515
{
    private static readonly Regex WhatsAppLinkTag = new(@"<a [^>]*href=""(?<href>https://wa\.me/[^""]+)""[^>]*>", RegexOptions.Compiled);

    private static readonly Regex CallLinkTag = new(@"<a [^>]*href=""(?<href>tel:[^""]+)""[^>]*>", RegexOptions.Compiled);

    // O Razor escreve o "+" como &#x2B; (HTML válido; o navegador lê "tel:+55…")
    private static string CallHref(string raw) => WebUtility.HtmlDecode(CallLinkTag.Match(raw).Groups["href"].Value);

    private static string WhatsAppHref(string raw) => WhatsAppLinkTag.Match(raw).Groups["href"].Value;

    // O texto que o WhatsApp mostra: a query do link lida do jeito que ele lê
    private static string MessageOf(string href)
    {
        var query = HttpUtility.ParseQueryString(new Uri(href).Query);
        CollectionAssert.AreEqual(new[] { "text" }, query.AllKeys);
        return query["text"];
    }

    [TestMethod]
    public async Task US004S01_ChamarNoWhatsApp_ConversaComONumeroEMensagemComTituloEEndereco()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33, photos: 2);

        (HttpResponseMessage response, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string href = WhatsAppHref(raw);
        StringAssert.StartsWith(href, "https://wa.me/5511912345678?text=", "conversa com o número do intermediário");
        Assert.AreEqual($"Olá! Tenho interesse no anúncio “Honda Civic 2018”: https://localhost/anuncio/{id}/honda-civic-2018", MessageOf(href));
        StringAssert.Matches(raw, new Regex(@"<a [^>]*data-contact-whatsapp[^>]*>[\s\S]*?Chamar no WhatsApp"));
    }

    [TestMethod]
    public async Task US004S02_Ligar_AbreOTelefoneComONumeroPronto()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        Assert.AreEqual("tel:+5511912345678", CallHref(raw));
        StringAssert.Matches(raw, new Regex(@"<a [^>]*data-contact-call[^>]*>[\s\S]*?Ligar"));
    }

    [TestMethod]
    public async Task US004S03_OContatoEVisivelSemLogin_NumeroEscritoEOsDoisBotoes()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018")); // GetRawAsync usa o visitante anônimo
        string visible = Text(raw);

        StringAssert.Contains(visible, "Fale com a Gazeta");
        StringAssert.Contains(visible, "(11) 91234-5678");
        StringAssert.Matches(raw, new Regex(@"<p [^>]*data-phone[^>]*>\(11\) 91234-5678</p>"));
        StringAssert.Contains(visible, "Ligar");
        StringAssert.Contains(visible, "Chamar no WhatsApp");
        Assert.IsFalse(Regex.IsMatch(raw, @"href=""/painel/entrar""[^>]*>[^<]*(Ligar|WhatsApp)"), "nada pede login para o contato");
    }

    [TestMethod]
    public async Task US004S03_TelefoneFixoDeDezDigitos_EscritoComTraco()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "1134567890");
        int id = await AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        StringAssert.Contains(Text(raw), "(11) 3456-7890");
        Assert.AreEqual("tel:+551134567890", CallHref(raw));
    }

    [TestMethod]
    public async Task US004S04_TituloComAcentosAspasEECerca_AMensagemMostraOTituloExato()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        const string title = "Sítio \"Boa Vista\" & Cia";
        int id = await AddAsync(site, title, 27);

        (_, string raw) = await GetRawAsync(site, Url(id, title));
        string href = WhatsAppHref(raw);

        Assert.AreEqual($"Olá! Tenho interesse no anúncio “{title}”: https://localhost{Url(id, title)}", MessageOf(href));
        StringAssert.Contains(href, "%26", "o & do título vai como %26 e não separa parâmetros");
        Assert.IsFalse(href.Contains('&', StringComparison.Ordinal) || href.Contains("&amp;", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task US004S05_WhatsApp_AbreEmNovaAbaSemPerderAPaginaDoAnuncio()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));
        string tag = WhatsAppLinkTag.Match(raw).Value;

        StringAssert.Contains(tag, "target=\"_blank\"", "nova aba: a página do anúncio continua aberta");
        StringAssert.Matches(tag, new Regex(@"rel=""[^""]*noopener[^""]*"""), "a aba nova não controla a do anúncio");
        StringAssert.StartsWith(WhatsAppHref(raw), "https://wa.me/", "o endereço do wa.me leva ao aplicativo ou, sem ele, ao WhatsApp Web");
        StringAssert.Contains(raw, ", abre em nova aba", "o nome acessível avisa da nova aba");
        Assert.IsFalse(CallLinkTag.Match(raw).Value.Contains("target=", StringComparison.Ordinal), "ligar não abre aba");
    }

    [TestMethod]
    public async Task Contato_BotoesComRotuloDeTexto_NaoSoIcone()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        StringAssert.Matches(raw, new Regex(@"<a [^>]*data-contact-call[^>]*><i [^>]*aria-hidden=""true""></i>Ligar<span class=""visually-hidden""> para \(11\) 91234-5678</span></a>"));
        StringAssert.Matches(raw, new Regex(@"<a [^>]*data-contact-whatsapp[^>]*><i [^>]*aria-hidden=""true""></i>Chamar no WhatsApp<span class=""visually-hidden"">, abre em nova aba</span></a>"));
        string block = Regex.Match(raw, @"<aside class=""card"" aria-labelledby=""contato-titulo""[\s\S]*?</aside>").Value;
        Assert.AreEqual(2, Regex.Matches(block, @"class=""btn btn-(primary|success) alvo-toque""").Count, "alvo de toque de 44 px nos dois botões");
    }

    [TestMethod]
    public async Task SemTelefoneConfigurado_OBlocoNaoAparece_EAPaginaContinuaInteira()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Honda Civic 2018", 33, photos: 2);

        (HttpResponseMessage response, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsFalse(Regex.IsMatch(raw, @"data-contact(?!-slot)|tel:|wa\.me|Fale com a Gazeta"), "sem número não há botão que leve a lugar nenhum");
        StringAssert.Contains(raw, "data-contact-slot", "a coluna do contato continua reservada para o Favoritar (5.5)");
        StringAssert.Contains(Text(raw), "Honda Civic 2018");
    }

    [TestMethod]
    public async Task Contato_NaoRegistraCliques_LinksComunsSemRastreio()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));
        string block = Regex.Match(raw, @"<aside class=""card"" aria-labelledby=""contato-titulo""[\s\S]*?</aside>").Value;

        Assert.AreNotEqual(string.Empty, block);
        Assert.AreEqual(2, Regex.Matches(block, @"<a ").Count);
        Assert.IsFalse(Regex.IsMatch(block, @"\b(onclick|onmousedown|ping|data-track\w*|data-analytics\w*)="), "sem evento, sem ping, sem marcador de métrica");
        foreach (Match link in Regex.Matches(block, @"href=""([^""]+)"""))
        {
            Assert.IsTrue(link.Groups[1].Value.StartsWith("tel:", StringComparison.Ordinal) || link.Groups[1].Value.StartsWith("https://wa.me/", StringComparison.Ordinal), "o link vai direto ao destino, sem passar pelo servidor: " + link.Groups[1].Value);
        }
    }

    [TestMethod]
    public async Task Contato_FicaAbaixoDoCorpoDoAnuncio_NaColunaLateral()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        // No celular as colunas empilham: a lateral (contato) vem depois da principal (galeria, preço e descrição)
        int body = raw.IndexOf("data-ad-body", StringComparison.Ordinal);
        int description = raw.IndexOf("ad-corpo__descricao", StringComparison.Ordinal);
        int contact = raw.IndexOf("data-contact>", StringComparison.Ordinal);
        Assert.IsTrue(body > 0 && description > body && contact > description, "ordem: corpo, descrição, contato");
        StringAssert.Matches(raw, new Regex(@"<div class=""col-12 col-lg-4"" data-contact-slot>\s*<aside"));
    }

    [TestMethod]
    public async Task MudarOTelefoneDoSite_MudaOContatoDosAnunciosNaHora()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33);
        (_, string before) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        await site.Harness.WithDbAsync(async db =>
        {
            db.SiteSettings.Single(s => s.Key == GazetaMarketplace.Core.Settings.SiteSettingKeys.Phone).Value = "21987654321";
            await db.SaveChangesAsync();
            return 0;
        });
        site.Harness.Factory.Services.GetRequiredService<GazetaMarketplace.Core.Settings.ISiteSettings>().Invalidate();
        (_, string after) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        StringAssert.Contains(Text(before), "(11) 91234-5678");
        StringAssert.Contains(Text(after), "(21) 98765-4321");
        Assert.AreEqual("tel:+5521987654321", CallHref(after));
        StringAssert.StartsWith(WhatsAppHref(after), "https://wa.me/5521987654321?text=");
    }

    [TestMethod]
    public async Task Vaga_ETambemTemOContato()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Pizzaiolo com experiência", 96, photos: 0, configure: ad =>
        {
            ad.SetPrice(280_000);
            ad.SetAttributes(new AdAttributes().Set("jobAreaIds", new[] { 1 }));
        });

        (_, string raw) = await GetRawAsync(site, Url(id, "Pizzaiolo com experiência"));

        Assert.IsTrue(CallLinkTag.IsMatch(raw));
        Assert.AreEqual("Olá! Tenho interesse no anúncio “Pizzaiolo com experiência”: https://localhost" + Url(id, "Pizzaiolo com experiência"), MessageOf(WhatsAppHref(raw)));
    }

    [TestMethod]
    public async Task EnderecoDaMensagem_VemDoSiteConfigurado_QuandoHa()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: new System.Collections.Generic.Dictionary<string, string> { ["Site:BaseUrl"] = "https://www.gazeta.example/" });
        await SetPhoneAsync(site, "11912345678");
        int id = await AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));

        Assert.AreEqual($"Olá! Tenho interesse no anúncio “Honda Civic 2018”: https://www.gazeta.example/anuncio/{id}/honda-civic-2018", MessageOf(WhatsAppHref(raw)));
        StringAssert.Contains(raw, $"<link rel=\"canonical\" href=\"https://www.gazeta.example/anuncio/{id}/honda-civic-2018\"");
    }
}

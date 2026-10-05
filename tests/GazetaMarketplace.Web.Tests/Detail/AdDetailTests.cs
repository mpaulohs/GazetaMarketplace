using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Detail;

/// <summary>A página pública do anúncio (US-003, S01 a S08; tarefa 5.2): endereço, indisponibilidade, galeria (a parte do servidor), características por grupo, data em São Paulo, SEO e privacidade.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdDetailTests
#pragma warning restore CA1515
{
    private static readonly DateTime Noon = new(2026, 9, 12, 15, 0, 0, DateTimeKind.Utc);

    private static string Text(string html) => System.Net.WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<(script|style)[\s\S]*?</\1>", " "), @"<[^>]+>", " ")).Trim() is { } t ? Regex.Replace(t, @"\s+", " ") : string.Empty;

    /// <summary>Grava um anúncio na situação pedida (com a data de publicação que o teste quiser) e <paramref name="photos"/> fotos de 1600 × 1200.</summary>
    private static async Task<int> AddAsync(DraftSite site, string title, int categoryId, int photos = 1, Action<Ad> configure = null, DateTime? at = null, byte status = AdStatus.Published)
    {
        await EnsureBrunoAsync(site);
        int author = await site.UserIdAsync(Ana);
        int admin = await site.UserIdAsync(PanelFixture.AdminEmail);
        DateTime when = at ?? Noon;
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(categoryId);
        ad.SetText(title, "Único dono, revisões feitas na concessionária.\nAceita troca.");
        ad.SetPrice(6_200_000);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        configure?.Invoke(ad);
        switch (status)
        {
            case AdStatus.InReview:
                ad.ApplyTransition(AdStatus.InReview, author, when, null);
                break;
            case AdStatus.Published:
                ad.ApplyTransition(AdStatus.InReview, author, when, null);
                ad.ApplyTransition(AdStatus.Published, admin, when, null);
                break;
            case AdStatus.Rejected:
                ad.ApplyTransition(AdStatus.InReview, author, when, null);
                ad.ApplyTransition(AdStatus.Rejected, admin, when, "Fotos escuras");
                break;
            case AdStatus.Archived:
                ad.ApplyTransition(AdStatus.Archived, admin, when, null);
                break;
        }

        return await site.Harness.WithDbAsync(async db =>
        {
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            for (int i = 0; i < photos; i++)
            {
                db.AdPhotos.Add(new AdPhoto { AdId = ad.Id, SortOrder = i, StorageKey = $"detalhe-{ad.Id}-{i}-{Guid.NewGuid():N}.webp", Width = 1600, Height = 1200, SizeBytes = 1000, CreatedAt = when });
            }

            await db.SaveChangesAsync();
            return ad.Id;
        });
    }

    private static Task<List<AdPhoto>> PhotosOfAsync(DraftSite site, int adId) =>
        site.Harness.WithDbAsync(db => db.AdPhotos.AsNoTracking().Where(p => p.AdId == adId).OrderBy(p => p.SortOrder).ToListAsync());

    private static async Task<(HttpResponseMessage Response, string Raw)> GetRawAsync(DraftSite site, string path)
    {
        using HttpClient visitor = site.Harness.Anonymous();
        HttpResponseMessage response = await visitor.GetAsync(path);
        return (response, await response.Content.ReadAsStringAsync());
    }

    private static string Url(int id, string title) => AdRoutes.Detail(id, title);

    private static void WithCar(Ad ad)
    {
        ad.SetAttributes(new AdAttributes().Set("brandId", 1).Set("modelId", 11).Set("modelYear", 2019).Set("versionId", 100).Set("km", 45000));
    }

    [TestMethod]
    public async Task US003S01_AbrirUmAnuncioCompleto_FotoDeCapaMiniaturasETudoDoTexto()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Honda Civic 2018", 33, photos: 6, configure: WithCar);
        List<AdPhoto> photos = await PhotosOfAsync(site, id);

        (HttpResponseMessage response, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));
        string html = WebUtility.HtmlDecode(raw);
        string visible = Text(raw);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(html, $"src=\"/fotos/{id}/{photos[0].Id}-1600.webp\"", "a foto de capa em destaque (versão grande)");
        Assert.AreEqual(6, Regex.Matches(html, @"data-gallery-thumb ").Count, "as miniaturas das 6 fotos");
        Assert.AreEqual(6, Regex.Matches(html, @"src=""/fotos/\d+/\d+-480\.webp""").Count);
        StringAssert.Matches(html, new Regex(@"<h1[^>]*>Honda Civic 2018</h1>"));
        StringAssert.Contains(visible, "R$ 62.000");
        StringAssert.Contains(visible, "Carros, vans e utilitários");
        StringAssert.Contains(visible, "Campinas/SP");
        StringAssert.Contains(visible, "Publicado em 12/09/2026");
        StringAssert.Contains(visible, "Único dono, revisões feitas na concessionária.");
        StringAssert.Contains(visible, "Aceita troca.");
        foreach (string spec in new[] { "Marca Honda", "Modelo Civic", "Ano 2019", "Quilometragem 45.000 km" })
        {
            StringAssert.Contains(visible, spec);
        }

        StringAssert.Contains(visible, "Características do veículo");
        StringAssert.Matches(html, new Regex(@"<nav aria-label=""Caminho de navegação""[\s\S]*Início[\s\S]*Automóveis, Peças e Acessórios[\s\S]*Carros, vans e utilitários[\s\S]*Honda Civic 2018"));
        Assert.IsTrue(Regex.IsMatch(html, @"data-contact-slot"), "o lugar do contato e do Favoritar fica reservado (5.3 e 5.5)");
        Assert.IsFalse(Regex.IsMatch(html, @"href=""tel:|wa\.me|Favoritar"), "contato e Favoritar chegam nas tarefas 5.3 e 5.5");
    }

    [TestMethod]
    public async Task US003S02_Galeria20Fotos_ServidorEntregaTodasAsMiniaturasEOContador_SomenteAPrimeiraFotoGrandeCarregaJa()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Carro com 20 fotos", 33, photos: 20);

        (_, string raw) = await GetRawAsync(site, Url(id, "Carro com 20 fotos"));
        string html = WebUtility.HtmlDecode(raw);

        Assert.AreEqual(20, Regex.Matches(html, @"data-gallery-thumb ").Count);
        CollectionAssert.AreEqual(Enumerable.Range(0, 20).Select(i => i.ToString()).ToArray(), Regex.Matches(html, @"data-gallery-thumb data-index=""(\d+)""").Select(m => m.Groups[1].Value).ToArray(), "na ordem da galeria");
        StringAssert.Contains(html, "data-gallery-counter");
        StringAssert.Contains(html, "1 de 20");
        StringAssert.Contains(html, "data-large=", "as demais fotos grandes só são buscadas quando escolhidas");
        Assert.AreEqual(1, Regex.Matches(html, @"<img[^>]*-1600\.webp").Count);
        foreach (Match thumb in Regex.Matches(html, @"<img src=""/fotos/\d+/\d+-480\.webp""[^>]*>"))
        {
            StringAssert.Contains(thumb.Value, "loading=\"lazy\"");
            StringAssert.Matches(thumb.Value, new Regex(@"width=""\d+"" height=""\d+"""), "o tamanho reservado evita a página pular");
        }

        Assert.IsTrue(Regex.IsMatch(html, @"<img[^>]*fetchpriority=""high"""), "a foto em destaque tem prioridade");
        StringAssert.Matches(html, new Regex(@"<img[^>]*src=""/fotos/\d+/\d+-1600\.webp""[^>]*width=""1600"" height=""1200"""));
    }

    [TestMethod]
    public async Task US003S03_Ampliar_SemJavaScriptAFotoEUmLinkParaAVersaoGrande_ComJavaScriptHaUmDialogComFechar()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Carro para ampliar", 33, photos: 3);
        List<AdPhoto> photos = await PhotosOfAsync(site, id);

        (_, string raw) = await GetRawAsync(site, Url(id, "Carro para ampliar"));
        string html = WebUtility.HtmlDecode(raw);

        StringAssert.Matches(html, new Regex($@"<a class=""ad-galeria__ampliar"" href=""/fotos/{id}/{photos[0].Id}-1600\.webp"" data-gallery-open>"));
        StringAssert.Matches(html, new Regex(@"<dialog[^>]*data-gallery-dialog[\s\S]*data-gallery-close>Fechar</button>"));
        Assert.IsFalse(Regex.IsMatch(html, @"<dialog[^>]*\sopen[\s>]"), "a janela só abre por ação do visitante");
        Assert.IsFalse(Regex.IsMatch(html, @"\son(click|error|load)="), "nenhum manipulador embutido (CSP)");
    }

    [TestMethod]
    public async Task US003S04_CategoriaSemFichaDeVeiculoNemDeTerreno_MostraCondicaoETipoDeProduto()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Vaso de cerâmica", 50, configure: ad => ad.SetAttributes(new AdAttributes().Set("conditionId", 2).Set("productType", "Vaso")));

        (_, string raw) = await GetRawAsync(site, Url(id, "Vaso de cerâmica"));
        string visible = Text(raw);

        StringAssert.Contains(visible, "Vaso de cerâmica");
        StringAssert.Contains(visible, "R$ 62.000");
        StringAssert.Contains(visible, "Campinas/SP");
        StringAssert.Contains(visible, "Único dono");
        StringAssert.Contains(visible, "Condição " + FieldLists.ProductCondition.Find(2).Label);
        StringAssert.Contains(visible, "Tipo de produto Vaso");
        StringAssert.Contains(visible, "Características");
        Assert.IsFalse(visible.Contains("Características do veículo", StringComparison.Ordinal));
        Assert.IsFalse(visible.Contains("Características do terreno", StringComparison.Ordinal));
        foreach (string vehicleField in new[] { "Marca", "Modelo", "Quilometragem", "Área (m²)" })
        {
            Assert.IsFalse(visible.Contains(vehicleField + " ", StringComparison.Ordinal), "sem o campo " + vehicleField);
        }
    }

    [TestMethod]
    [DataRow(33, "Características do veículo")]
    [DataRow(36, "Características do veículo")]
    [DataRow(34, "Características do veículo")]
    [DataRow(35, "Características do veículo")]
    [DataRow(37, "Características do veículo")]
    [DataRow(30, "Características do terreno")]
    [DataRow(26, "Características")]
    [DataRow(86, "Características")]
    [DataRow(38, "Características")]
    public async Task US003S04_TituloDoBlocoDeCaracteristicas_VemDoGrupoDeCampos(int categoryId, string expected)
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Anúncio do grupo", categoryId, configure: ad => ad.SetAttributes(AttributesFor(categoryId)));

        (_, string raw) = await GetRawAsync(site, Url(id, "Anúncio do grupo"));

        Match heading = Regex.Match(raw, @"<h2 class=""fs-5"" id=""ad-corpo-caracteristicas"">([^<]+)</h2>");
        Assert.IsTrue(heading.Success, $"categoria {categoryId}: o bloco de características existe");
        Assert.AreEqual(expected, WebUtility.HtmlDecode(heading.Groups[1].Value).Trim(), $"categoria {categoryId}");
    }

    private static AdAttributes AttributesFor(int categoryId) => categoryId switch
    {
        33 => new AdAttributes().Set("km", 1000),
        36 => new AdAttributes().Set("km", 1000),
        34 => new AdAttributes().Set("km", 1000),
        35 => new AdAttributes().Set("km", 1000),
        37 => new AdAttributes().Set("hoursOfUse", 100),
        30 => new AdAttributes().Set("areaM2", 450m),
        26 => new AdAttributes().Set("bedrooms", 2),
        _ => new AdAttributes().Set("conditionId", 2)
    };

    [TestMethod]
    public async Task US003S05_UmaUnicaFoto_SemMiniaturasSetasNemContador_MasAindaAmplia()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Carro com uma foto", 33, photos: 1);

        (_, string raw) = await GetRawAsync(site, Url(id, "Carro com uma foto"));
        string html = WebUtility.HtmlDecode(raw);

        StringAssert.Contains(html, "data-gallery-main");
        foreach (string absent in new[] { "data-gallery-thumbs", "data-gallery-thumb ", "data-gallery-prev", "data-gallery-next", "data-gallery-counter", "data-gallery-dialog-prev", "data-gallery-dialog-next" })
        {
            Assert.IsFalse(html.Contains(absent, StringComparison.Ordinal), "uma foto só não tem " + absent);
        }

        StringAssert.Contains(html, "data-gallery-open", "ampliar continua valendo");
    }

    [TestMethod]
    public async Task US003S06_Arquivado_Da404ComAMensagem_LinksParaInicioECategoria_SemTituloFotosNemDescricao()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Honda Civic 2018", 33, photos: 3, status: AdStatus.Archived);
        string url = Url(id, "Honda Civic 2018");

        (HttpResponseMessage response, string raw) = await GetRawAsync(site, url);
        string html = WebUtility.HtmlDecode(raw);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        StringAssert.Contains(Text(raw), "Este anúncio não está mais disponível");
        StringAssert.Matches(html, new Regex(@"<a href=""/"">página inicial</a>"));
        string categorySlug = (await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None)).Find(33).Slug;
        StringAssert.Matches(html, new Regex($@"<a href=""/categoria/{categorySlug}"" data-archived-category>Carros, vans e utilitários</a>"));
        foreach (string leak in new[] { "Honda Civic 2018", "Único dono", "/fotos/", "R$ 62.000", "data-gallery" })
        {
            Assert.IsFalse(html.Replace("Anúncio não disponível", string.Empty, StringComparison.Ordinal).Contains(leak, StringComparison.Ordinal), "a página de indisponível não revela " + leak);
        }
    }

    [TestMethod]
    public async Task NaoPublicado_E_Inexistente_TemAMesmaResposta_ArquivadoSoAcrescentaOLinkDaCategoria()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int draft = await AddAsync(site, "Rascunho secreto", 33, status: AdStatus.Draft);
        int review = await AddAsync(site, "Em revisão secreta", 33, status: AdStatus.InReview);
        int rejected = await AddAsync(site, "Rejeitado secreto", 33, status: AdStatus.Rejected);
        int archived = await AddAsync(site, "Arquivado secreto", 33, status: AdStatus.Archived);
        int missing = archived + 1000;

        List<(HttpStatusCode Status, string Body, string Name)> answers = [];
        foreach ((int id, string title, string name) in new[] { (draft, "Rascunho secreto", "rascunho"), (review, "Em revisão secreta", "em revisão"), (rejected, "Rejeitado secreto", "rejeitado"), (missing, "qualquer", "inexistente") })
        {
            (HttpResponseMessage response, string raw) = await GetRawAsync(site, Url(id, title));
            answers.Add((response.StatusCode, raw, name));
        }

        foreach ((HttpStatusCode status, string body, string name) in answers)
        {
            Assert.AreEqual(HttpStatusCode.NotFound, status, name);
            Assert.AreEqual(answers[0].Body, body, $"{name} responde igual ao rascunho (nem o tamanho da página diferencia)");
            Assert.IsFalse(body.Contains("secret", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(body.Contains("data-archived-category", StringComparison.Ordinal));
        }

        (HttpResponseMessage archivedResponse, string archivedBody) = await GetRawAsync(site, Url(archived, "Arquivado secreto"));
        Assert.AreEqual(HttpStatusCode.NotFound, archivedResponse.StatusCode, "nunca 410");
        StringAssert.Contains(archivedBody, "data-archived-category");
        StringAssert.Contains(archivedBody, "Este anúncio não está mais disponível");
        Assert.IsFalse(archivedBody.Contains("secret", StringComparison.OrdinalIgnoreCase), "o título do arquivado não aparece");
        StringAssert.Contains(answers[0].Body, "<meta name=\"robots\" content=\"noindex\"");
    }

    [TestMethod]
    public async Task US003S07_FotoQueNaoCarrega_OServidorEntregaOLugarDaMensagemEAsOutrasFotosNavegaveis()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Carro com foto quebrada", 33, photos: 3);

        (_, string raw) = await GetRawAsync(site, Url(id, "Carro com foto quebrada"));
        string html = WebUtility.HtmlDecode(raw);

        StringAssert.Matches(html, new Regex(@"<div class=""ad-galeria__indisponivel media-reservada"" role=""status"" hidden data-gallery-broken><span class=""fw-bold"">Foto indisponível</span></div>"));
        Assert.AreEqual(3, Regex.Matches(html, @"data-gallery-thumb ").Count, "as outras fotos continuam com a miniatura");
        StringAssert.Contains(html, "data-gallery-prev");
        StringAssert.Contains(html, "data-gallery-next");
    }

    [TestMethod]
    public async Task US003S08_CelularEstreito_GaleriaComMiniaturasEmFaixaPropria_ContatoReservado()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Carro no celular", 33, photos: 20);

        (_, string raw) = await GetRawAsync(site, Url(id, "Carro no celular"));
        string css = await site.Harness.Anonymous().GetStringAsync("/css/pages/ad-detail.css");

        StringAssert.Contains(raw, "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"");
        Assert.IsFalse(Regex.IsMatch(raw, @"style=""[^""]*width\s*:\s*\d+px"), "nenhuma largura fixa em pixels");
        StringAssert.Matches(css, new Regex(@"\.ad-galeria__miniaturas\s*\{[^}]*overflow-x:\s*auto"), "as miniaturas rolam na própria faixa, não na página");
        StringAssert.Matches(css, new Regex(@"\.ad-galeria__destaque\s*\{[^}]*touch-action:\s*pan-y"), "deslizar na foto não rouba a rolagem vertical");
        StringAssert.Matches(css, new Regex(@"\.ad-galeria__seta\s*\{[^}]*min-width:\s*44px;[^}]*min-height:\s*44px"), "alvo de toque de 44 px");
        Assert.IsFalse(css.Contains("!important", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Endereco_SlugErradoOuFaltando_Redireciona301ParaOEnderecoAtual_ETituloEditadoNaoQuebraOLink()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Honda Civic 2018", 33);
        string canonical = Url(id, "Honda Civic 2018");
        Assert.AreEqual($"/anuncio/{id}/honda-civic-2018", canonical);

        foreach (string wrong in new[] { $"/anuncio/{id}", $"/anuncio/{id}/", $"/anuncio/{id}/titulo-antigo", $"/anuncio/{id}/HONDA-CIVIC-2018", $"/anuncio/{id}/honda-civic-2018/" })
        {
            (HttpResponseMessage response, _) = await GetRawAsync(site, wrong);
            Assert.AreEqual(HttpStatusCode.MovedPermanently, response.StatusCode, wrong);
            Assert.AreEqual(canonical, response.Headers.Location!.OriginalString, wrong);
        }

        (HttpResponseMessage withQuery, _) = await GetRawAsync(site, $"/anuncio/{id}/antigo?origem=email");
        Assert.AreEqual(canonical + "?origem=email", withQuery.Headers.Location!.OriginalString);
        Assert.AreEqual(HttpStatusCode.OK, (await GetRawAsync(site, canonical)).Response.StatusCode, "o endereço atual responde direto");

        await site.Harness.WithDbAsync(async db =>
        {
            Ad ad = await db.Ads.SingleAsync(a => a.Id == id);
            ad.SetText("Honda Civic 2019 revisado", ad.Description);
            await db.SaveChangesAsync();
            return 0;
        });
        (HttpResponseMessage renamed, _) = await GetRawAsync(site, canonical);
        Assert.AreEqual(HttpStatusCode.MovedPermanently, renamed.StatusCode, "o link salvo continua levando ao anúncio");
        Assert.AreEqual($"/anuncio/{id}/honda-civic-2019-revisado", renamed.Headers.Location!.OriginalString);
    }

    [TestMethod]
    [DataRow("/anuncio/abc/qualquer")]
    [DataRow("/anuncio/0/x")]
    [DataRow("/anuncio/-1/x")]
    [DataRow("/anuncio/99999999999/x")]
    public async Task Endereco_IdQueNaoEUmNumeroValido_Da404SemErro(string path)
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpResponseMessage response, _) = await GetRawAsync(site, path);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    [DataRow(23, 59, 12, "12/09/2026", DisplayName = "23:59 UTC = 20:59 em São Paulo: mesmo dia")]
    [DataRow(2, 0, 13, "12/09/2026", DisplayName = "02:00 UTC = 23:00 em São Paulo: dia anterior")]
    [DataRow(3, 0, 13, "13/09/2026", DisplayName = "03:00 UTC = 00:00 em São Paulo: já é o dia 13")]
    public async Task DataDePublicacao_EmSaoPaulo_NaoNoFusoDoServidor(int hour, int minute, int day, string expected)
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Anúncio da data", 33, at: new DateTime(2026, 9, day, hour, minute, 0, DateTimeKind.Utc));

        (_, string raw) = await GetRawAsync(site, Url(id, "Anúncio da data"));

        StringAssert.Contains(Text(raw), "Publicado em " + expected);
    }

    [TestMethod]
    public async Task Vaga_NaoTemFotos_BlocoVagaDeEmpregoComAreas_SalarioEInformacoesAdicionais()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Pizzaiolo com experiência", 96, photos: 0, configure: ad =>
        {
            ad.SetPrice(280_000);
            ad.SetAttributes(new AdAttributes().Set("jobAreaIds", new[] { 2, 1 }));
        });

        (_, string raw) = await GetRawAsync(site, Url(id, "Pizzaiolo com experiência"));
        string html = WebUtility.HtmlDecode(raw);
        string visible = Text(raw);

        StringAssert.Contains(html, "data-job-block");
        StringAssert.Contains(visible, "Vaga de emprego");
        StringAssert.Contains(visible, FieldLists.JobArea.Find(2).Label);
        StringAssert.Contains(visible, FieldLists.JobArea.Find(1).Label);
        StringAssert.Contains(visible, "Salário R$ 2.800");
        StringAssert.Contains(visible, "Informações adicionais");
        Assert.IsFalse(Regex.IsMatch(html, @"data-gallery|<img|/fotos/"), "vagas não têm foto, galeria nem ampliar");
        Assert.IsFalse(raw.Contains("ad-detail.js", StringComparison.Ordinal), "sem galeria, sem o script dela");
    }

    [TestMethod]
    public async Task Servico_TipoNoLugarDoPreco_EInformacoesAdicionais()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Diarista", 66, photos: 2, configure: ad =>
        {
            ad.SetPrice(null);
            ad.SetAttributes(new AdAttributes().Set("serviceTypeId", 1));
        });

        (_, string raw) = await GetRawAsync(site, Url(id, "Diarista"));
        string visible = Text(raw);

        StringAssert.Contains(raw, "data-ad-value=\"tipo\"");
        StringAssert.Contains(visible, FieldLists.ServiceType.Find(1).Label);
        StringAssert.Contains(visible, "Informações adicionais");
        Assert.IsFalse(visible.Contains("R$", StringComparison.Ordinal), "Serviços não têm preço");
    }

    [TestMethod]
    public async Task Seo_TituloDescricaoECanonico_ENoindexSoNaIndisponivel()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Honda Civic 2018", 33, configure: ad => ad.SetText("Honda Civic 2018", "Único dono,\n\n  revisões feitas   na concessionária."));

        (_, string raw) = await GetRawAsync(site, Url(id, "Honda Civic 2018"));
        string html = WebUtility.HtmlDecode(raw);

        StringAssert.Contains(html, "<title>Honda Civic 2018 · GazetaMarketplace</title>");
        StringAssert.Contains(html, "<meta name=\"description\" content=\"Único dono, revisões feitas na concessionária.\" />");
        StringAssert.Contains(html, $"<link rel=\"canonical\" href=\"https://localhost/anuncio/{id}/honda-civic-2018\" />");
        Assert.IsFalse(html.Contains("noindex", StringComparison.Ordinal), "anúncio publicado pode ser indexado");
    }

    [TestMethod]
    public async Task Privacidade_NemNomeNemEmailNemTelefoneDoAutor_NaPagina()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Anúncio privado", 33, photos: 2);
        AppUserSnapshot author = await site.Harness.WithDbAsync(async db =>
        {
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == Ana);
            return new AppUserSnapshot(user.FullName, user.Email, user.PhoneNumber);
        });

        (_, string raw) = await GetRawAsync(site, Url(id, "Anúncio privado"));

        Assert.IsFalse(raw.Contains(author.Email, StringComparison.OrdinalIgnoreCase), "e-mail");
        Assert.IsFalse(WebUtility.HtmlDecode(raw).Contains(author.FullName, StringComparison.Ordinal), "nome");
        if (!string.IsNullOrEmpty(author.Phone))
        {
            Assert.IsFalse(raw.Contains(author.Phone, StringComparison.Ordinal), "telefone");
        }

        CollectionAssert.DoesNotContain(typeof(AdDetail).GetProperties().Select(p => p.Name).ToArray(), "AuthorId");
        CollectionAssert.DoesNotContain(typeof(AdPageViewModel).GetProperties().Select(p => p.Name).ToArray(), "Author");
    }

    private sealed record AppUserSnapshot(string FullName, string Email, string Phone);

    [TestMethod]
    public async Task TextoDoAnuncio_NuncaViraHtml_QuebrasDeLinhaSaoSoTexto()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "<img src=x onerror=alert(1)> título", 33, configure: ad => ad.SetText("<img src=x onerror=alert(1)> título", "<script>alert('x')</script>\nsegunda linha <b>negrito</b>"));

        (_, string raw) = await GetRawAsync(site, $"/anuncio/{id}/{AdRoutes.Detail(id, "<img src=x onerror=alert(1)> título").Split('/')[3]}");

        Assert.IsFalse(raw.Contains("<script>alert", StringComparison.Ordinal));
        Assert.IsFalse(raw.Contains("<img src=x", StringComparison.Ordinal));
        Assert.IsFalse(raw.Contains("<b>negrito</b>", StringComparison.Ordinal));
        StringAssert.Contains(raw, "&lt;script&gt;alert(&#x27;x&#x27;)&lt;/script&gt;");
        StringAssert.Contains(raw, "content=\"&lt;script&gt;alert(&#x27;x&#x27;)&lt;/script&gt; segunda linha &lt;b&gt;negrito&lt;/b&gt;\"", "a meta description também sai codificada");
    }

    [TestMethod]
    public async Task Falha_Da503ComMensagemCodigoDeReferenciaESemDetalheTecnico()
    {
        using DraftSite site = await DraftSite.StartAsync(services: s => s.AddScoped<IPublishedAdReader, FailingReader>());

        (HttpResponseMessage response, string raw) = await GetRawAsync(site, "/anuncio/1/qualquer");
        string html = WebUtility.HtmlDecode(raw);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        StringAssert.Contains(Text(raw), "Não foi possível carregar o anúncio. Tente novamente.");
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/anuncio/1/qualquer""[^>]*>Tentar novamente</a>"));
        StringAssert.Contains(html, "Código de referência");
        foreach (string leak in new[] { "segredo-do-banco", "db.interno", "InvalidOperationException", "   at " })
        {
            Assert.IsFalse(html.Contains(leak, StringComparison.Ordinal), "sem detalhe técnico: " + leak);
        }

        Assert.IsTrue(site.Harness.Factory.Logs.Events.Any(e => e.Exception?.Message.Contains("segredo-do-banco", StringComparison.Ordinal) == true), "a causa fica no log");
    }

    private sealed class FailingReader : IPublishedAdReader
    {
        public Task<Ad> FindPublishedAsync(int id, CancellationToken cancellationToken) => throw new InvalidOperationException("segredo-do-banco: Server=db.interno;Password=abc");

        public Task<int?> FindArchivedCategoryIdAsync(int id, CancellationToken cancellationToken) => throw new InvalidOperationException("segredo-do-banco");
    }

    [TestMethod]
    public async Task OsCardsDaVitrine_LevamAoDetalhe_QueResponde200()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAsync(site, "Anúncio do card", 33);
        ShowcaseRow row = new(id, "Anúncio do card", 33, 6_200_000, "{}", "Campinas", "SP", null, null, null);
        site.Harness.Factory.Services.GetRequiredService<StubShowcaseRepository>().Rows.Add(row);

        (_, string home) = await GetRawAsync(site, "/");
        string href = Regex.Match(home, @"class=""stretched-link ad-card__link"" href=""([^""]+)""").Groups[1].Value;

        Assert.AreEqual($"/anuncio/{id}/anuncio-do-card", href);
        Assert.AreEqual(HttpStatusCode.OK, (await GetRawAsync(site, href)).Response.StatusCode, "o card leva a uma página que existe");
    }
}

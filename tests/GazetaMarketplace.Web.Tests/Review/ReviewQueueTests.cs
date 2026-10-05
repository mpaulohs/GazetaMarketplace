using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Photos;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Review;

/// <summary>A fila de revisão e a pré-visualização do Administrador (US-010-S01, S02, S06 e S09; tarefa 4.1).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ReviewQueueTests
#pragma warning restore CA1515
{
    private const string Queue = "/painel/anuncios/fila";

    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<[^>]+>", " "), @"\s+", " ")).Trim();

    [TestMethod]
    public async Task US010S01_VerAFilaDeRevisao_DoMaisAntigoParaOMaisRecente_ComTituloAutorCategoriaEDataDeEnvio()
    {
        using DraftSite site = await DraftSite.StartAsync();
        // Gravados fora de ordem de propósito: a fila ordena pela data de envio, não pela ordem de gravação
        await AddInReviewAsync(site, Ana, "Moto para retirar peças", 36, Day(30));
        await AddInReviewAsync(site, Bruno, "Casa com quintal", 27, Day(28));
        await AddInReviewAsync(site, Ana, "Honda Civic 2018", 33, Day(29));
        // Fora da fila: as outras quatro situações
        await site.AddAdAsync(Ana, AdStatus.Draft, "Rascunho de teste");
        await site.AddAdAsync(Ana, AdStatus.Published, "Publicado de teste");
        await site.AddAdAsync(Ana, AdStatus.Rejected, "Rejeitado de teste");
        await site.AddAdAsync(Ana, AdStatus.Archived, "Arquivado de teste");

        HttpResponseMessage response = await site.Admin.GetAsync(Queue);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string[] titles = [.. Regex.Matches(html, @"<a [^>]*pre-visualizacao[^>]*>([^<]+)</a>").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value))];
        CollectionAssert.AreEqual(new[] { "Casa com quintal", "Honda Civic 2018", "Moto para retirar peças" }, titles, "do mais antigo para o mais recente");
        foreach (string outside in new[] { "Rascunho de teste", "Publicado de teste", "Rejeitado de teste", "Arquivado de teste" })
        {
            Assert.IsFalse(html.Contains(outside, StringComparison.Ordinal), outside + " não está em revisão");
        }

        string visible = Text(html);
        StringAssert.Contains(visible, "3 anúncios aguardando revisão, do mais antigo ao mais recente");
        StringAssert.Contains(visible, "Fila de revisão (3)");
        StringAssert.Matches(html, new Regex(@"Casa com quintal</a>\s*</th>\s*<td[^>]*>Bruno Lima</td>\s*<td[^>]*>Casas</td>\s*<td[^>]*><time datetime=""2026-09-28T13:00:00Z"" title=""28/09/2026 10:00"">28/09/2026</time>"));
        StringAssert.Matches(html, new Regex(@"Honda Civic 2018</a>\s*</th>\s*<td[^>]*>Ana Souza</td>\s*<td[^>]*>Carros, vans e utilitários</td>\s*<td[^>]*><time[^>]*>29/09/2026</time>"));
        StringAssert.Matches(html, new Regex(@"<th scope=""col"">Título</th>\s*<th scope=""col"">Autor</th>\s*<th scope=""col"">Categoria</th>\s*<th scope=""col"">Enviado em</th>"));
        Assert.IsFalse(visible.Contains("provisória", StringComparison.Ordinal), "a página provisória da 1.1 foi trocada");
    }

    [TestMethod]
    public async Task Fila_MesmaDataDeEnvio_DesempataPeloIdEOReenvioContaComoNovoEnvio()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await AddInReviewAsync(site, Ana, "Segundo no empate", 86, Day(10));
        await AddInReviewAsync(site, Ana, "Primeiro no empate", 86, Day(9));
        await AddInReviewAsync(site, Ana, "Terceiro no empate", 86, Day(10));
        // Rejeitado e reenviado depois de todos: a fila usa a data do último envio, então ele vai para o fim
        await AddInReviewAsync(site, Bruno, "Reenviado", 86, Day(12), ad =>
        {
            int author = ad.AuthorId;
            ad.ApplyTransition(AdStatus.InReview, author, Day(1), null);
            ad.ApplyTransition(AdStatus.Rejected, 1, Day(2), "Fotos escuras");
        });
        string html = await site.Admin.GetStringAsync(Queue);

        string[] titles = [.. Regex.Matches(html, @"<a [^>]*pre-visualizacao[^>]*>([^<]+)</a>").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value))];

        // Enviado pela primeira vez no dia 1 e reenviado no dia 12: vale o último envio
        CollectionAssert.AreEqual(new[] { "Primeiro no empate", "Segundo no empate", "Terceiro no empate", "Reenviado" }, titles);
    }

    [TestMethod]
    public async Task US010S06_FilaVazia_MostraAMensagem_SemTabela()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await site.AddAdAsync(Ana, AdStatus.Draft);
        await site.AddAdAsync(Ana, AdStatus.Published);

        string html = await site.Admin.GetStringAsync(Queue);

        StringAssert.Matches(html, new Regex(@"role=""status""[^>]*>Nenhum anúncio aguardando revisão<"));
        Assert.IsFalse(html.Contains("<table", StringComparison.Ordinal));
        StringAssert.Contains(Text(html), "Fila de revisão (0)");
    }

    [TestMethod]
    public async Task US010S09_RedatorNaoPodeRevisarAnuncios_NemAFilaNemAPreVisualizacao()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddInReviewAsync(site, Ana, "Anúncio da Ana", 33, Day(29));

        HttpResponseMessage queue = await site.Writer.GetAsync(Queue);
        HttpResponseMessage preview = await site.Writer.GetAsync(PreviewUrl(adId));

        foreach (HttpResponseMessage denied in new[] { queue, preview })
        {
            Assert.AreEqual(HttpStatusCode.Redirect, denied.StatusCode);
            StringAssert.StartsWith(denied.Destination(), "/painel/acesso-negado");
            string page = await (await site.Writer.GetAsync(denied.Destination())).TextAsync();
            StringAssert.Contains(page, "Você não tem permissão para acessar esta página");
            Assert.IsFalse(page.Contains("Publicar", StringComparison.Ordinal) || page.Contains("Rejeitar", StringComparison.Ordinal), "sem botões de decisão");
        }
    }

    [TestMethod]
    public async Task SemLogin_VaiParaAEntrada_NaFilaENaPreVisualizacao()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddInReviewAsync(site, Ana, "Anúncio", 33, Day(29));
        using HttpClient anonymous = site.Harness.Anonymous();

        foreach (string url in new[] { Queue, PreviewUrl(adId) })
        {
            HttpResponseMessage response = await anonymous.GetAsync(url);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, url);
            StringAssert.StartsWith(response.Destination(), "/painel/entrar", url);
        }
    }

    [TestMethod]
    public async Task Fila_TextoDoUsuarioSaiCodificado_EOMenuMarcaAnuncios()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await AddInReviewAsync(site, Ana, "<script>alert(1)</script> & \"aspas\"", 33, Day(29));

        string raw = await site.Admin.GetStringAsync(Queue);
        string html = WebUtility.HtmlDecode(raw);

        Assert.IsFalse(raw.Contains("<script>alert", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(raw, "&lt;script&gt;alert(1)&lt;/script&gt;");
        StringAssert.Matches(html, new Regex(@"<a class=""nav-link[^""]*active[^""]*"" href=""/painel/anuncios"" aria-current=""page"">Anúncios</a>"), "o item Anúncios do menu fica ativo na fila");
        StringAssert.Matches(html, new Regex(@"href=""/painel/anuncios""[^>]*>Todos os anúncios</a>"));
    }

    [TestMethod]
    public async Task Fila_FalhaAoCarregar_MostraErroComTentarNovamente_SemDetalheTecnico()
    {
        using DraftSite site = await DraftSite.StartAsync(services: services =>
        {
            services.RemoveAll<IReviewQueue>();
            services.AddSingleton<IReviewQueue>(new FailingQueue());
        });

        HttpResponseMessage response = await site.Admin.GetAsync(Queue);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        StringAssert.Contains(html, "Não foi possível carregar os anúncios");
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/painel/anuncios/fila""[^>]*>\s*Tentar novamente\s*</a>"));
        StringAssert.Matches(html, new Regex(@"Código de referência:[\s\S]*?<code>[^<]+</code>"));
        Assert.IsFalse(html.Contains("segredo-do-banco", StringComparison.Ordinal), "a mensagem da exceção não vaza");
    }

    [TestMethod]
    public async Task US010S02_PreVisualizar_MostraFaixa_CorpoCompleto_FotosEContato_EOsTresBotoes()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        DraftSite site = photos.Site;
        await SetPhoneAsync(site, "11912345678");
        int adId = await AddInReviewAsync(site, Ana, "Honda Civic 2018", 33, Day(29), ad =>
        {
            ad.SetText("Honda Civic 2018", "Único dono\nRevisões em dia");
            ad.SetAttributes(new AdAttributes().Set("brandId", 1).Set("modelId", 11).Set("modelYear", 2019).Set("versionId", 100).Set("km", 45000));
        });
        await photos.AddRowsAsync(adId, 3);

        HttpResponseMessage response = await site.Admin.GetAsync(PreviewUrl(adId));
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        string visible = Text(html);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(1, Regex.Matches(html, @"<h1\b").Count);
        StringAssert.Matches(html, new Regex(@"<h1[^>]*>Pré-visualização do anúncio</h1>\s*<div class=""alert alert-info"" role=""status""[^>]*>[\s\S]*?Pré-visualização — ainda não publicado</div>"));
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/painel/anuncios/" + adId + @"/editar""[^>]*>Editar</a>"));
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/painel/anuncios/fila""[^>]*>[\s\S]*?Fila de revisão</a>"));
        // Os botões da S02: Publicar, Rejeitar e Editar, mais "Arquivar" (US-011-S05); "Despublicar" só vale para anúncio publicado
        StringAssert.Matches(html, new Regex(@"<a class=""btn btn-primary[^""]*"" href=""/painel/anuncios/" + adId + @"/publicar"">Publicar</a>"));
        StringAssert.Matches(html, new Regex(@"<a class=""btn btn-outline-danger[^""]*"" href=""/painel/anuncios/" + adId + @"/rejeitar"">Rejeitar</a>"));
        StringAssert.Matches(html, new Regex(@"<a class=""btn btn-outline-danger[^""]*"" href=""/painel/anuncios/" + adId + @"/arquivar"">Arquivar</a>"));
        Assert.IsFalse(visible.Contains("Despublicar", StringComparison.Ordinal), "Despublicar só existe para anúncio publicado");

        // O mesmo corpo da página pública: título, valor, local, descrição e características reais
        StringAssert.Matches(html, new Regex(@"<h2 [^>]*>Honda Civic 2018</h2>"));
        StringAssert.Contains(visible, "R$ 62.000");
        StringAssert.Contains(visible, "Campinas/SP");
        StringAssert.Contains(visible, "Carros, vans e utilitários");
        StringAssert.Matches(html, new Regex(@"<dt[^>]*>Marca</dt>\s*<dd[^>]*>Honda</dd>"));
        StringAssert.Matches(html, new Regex(@"<dt[^>]*>Modelo</dt>\s*<dd[^>]*>Civic</dd>"));
        StringAssert.Matches(html, new Regex(@"<dt[^>]*>Ano</dt>\s*<dd[^>]*>2019</dd>"));
        StringAssert.Matches(html, new Regex(@"<dt[^>]*>Versão</dt>\s*<dd[^>]*>LX</dd>"));
        StringAssert.Matches(html, new Regex(@"<dt[^>]*>Quilometragem</dt>\s*<dd[^>]*>45\.000 km</dd>"));
        StringAssert.Matches(html, new Regex(@"class=""ad-corpo__descricao[^>]*>Único dono\nRevisões em dia</p>"));
        // Fotos simples, sem JavaScript: a de destaque em tamanho grande e 3 miniaturas que levam à versão grande
        Assert.AreEqual(3, Regex.Matches(html, @"<a class=""ad-fotos__miniatura"" href=""/fotos/" + adId + @"/\d+-1600\.webp""").Count);
        StringAssert.Matches(html, new Regex(@"<img [^>]*src=""/fotos/" + adId + @"/\d+-1600\.webp""[^>]*alt=""Foto 1 de 3"""));
        // Contato da Gazeta com o telefone do site
        StringAssert.Contains(visible, "Fale com a Gazeta");
        StringAssert.Contains(visible, "(11) 91234-5678");
        StringAssert.Contains(html, "href=\"tel:+5511912345678\"");
        StringAssert.Contains(html, "href=\"https://wa.me/5511912345678\"");
        Assert.IsFalse(html.Contains("data-location-manual", StringComparison.Ordinal), "CEP conferido: sem selo");
    }

    [TestMethod]
    public async Task PreVisualizar_CidadeEUfManuais_MostraOSeloInformativoComCep()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int manual = await AddInReviewAsync(site, Ana, "Casa manual", 27, Day(29), ad => ad.SetLocation("13015100", "Campinas", "SP", true));
        int checkedCep = await AddInReviewAsync(site, Ana, "Casa conferida", 27, Day(29));

        string withBadge = await site.Admin.GetStringAsync(PreviewUrl(manual));
        string without = await site.Admin.GetStringAsync(PreviewUrl(checkedCep));

        StringAssert.Contains(Text(withBadge), "Cidade/UF informadas manualmente (CEP não conferido) CEP 13015100 · Campinas/SP");
        Assert.IsFalse(without.Contains("informadas manualmente", StringComparison.Ordinal));
        StringAssert.Contains(withBadge, "Editar", "o selo é só informativo: não bloqueia nada");
    }

    [TestMethod]
    public async Task PreVisualizar_SemTelefoneDoSite_AvisaELevaAConfiguracoes()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddInReviewAsync(site, Ana, "Sem telefone", 33, Day(29));

        string html = await site.Admin.GetStringAsync(PreviewUrl(adId));

        StringAssert.Contains(Text(html), "O telefone/WhatsApp do site ainda não foi configurado.");
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/painel/configuracoes""[^>]*>Configurações</a>"));
        Assert.IsFalse(html.Contains("tel:", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task PreVisualizar_Vagas_MostraOBlocoVagaDeEmprego_ComAsAreas_SemFoto_ESalario()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddInReviewAsync(site, Ana, "Pizzaiolo", 96, Day(29), ad =>
        {
            ad.SetPrice(280_000);
            ad.SetAttributes(new AdAttributes().Set("jobAreaIds", new[] { 1, 2 }));
        });

        string html = await site.Admin.GetStringAsync(PreviewUrl(adId));
        string visible = Text(html);

        StringAssert.Matches(html, new Regex(@"<section class=""media-reservada ad-vaga[^>]*data-job-block>[\s\S]*?Vaga de emprego</h2>\s*<ul[^>]*>\s*<li>[^<]+</li>\s*<li>[^<]+</li>"));
        Assert.IsFalse(html.Contains("<img", StringComparison.Ordinal), "vagas não têm foto");
        Assert.IsFalse(html.Contains("ad-fotos", StringComparison.Ordinal));
        StringAssert.Contains(visible, "Salário R$ 2.800");
        Assert.IsFalse(Regex.IsMatch(html, @"<dt[^>]*>Área</dt>"), "as áreas aparecem no bloco, não repetidas nas características");
    }

    [TestMethod]
    public async Task PreVisualizar_RotuloDoCampoMudou_AsAreasEOTipoContinuamNoLugar_PorqueOCodigoUsaAChave()
    {
        // Um leitor que devolve os mesmos campos com outros rótulos: se a tela procurasse por "Área" ou "Tipo", o bloco da vaga sumiria e o valor do serviço viraria "R$"
        using DraftSite site = await DraftSite.StartAsync(services: s => s.AddScoped<IAdSpecsReader, RelabelledSpecsReader>());
        int jobId = await AddInReviewAsync(site, Ana, "Pizzaiolo", 96, Day(29), ad =>
        {
            ad.SetPrice(280_000);
            ad.SetAttributes(new AdAttributes().Set("jobAreaIds", new[] { 1, 2 }));
        });
        int serviceId = await AddInReviewAsync(site, Ana, "Diarista", 66, Day(29), ad =>
        {
            ad.SetPrice(null);
            ad.SetAttributes(new AdAttributes().Set("serviceTypeId", 1));
        });

        string job = await site.Admin.GetStringAsync(PreviewUrl(jobId));
        string service = await site.Admin.GetStringAsync(PreviewUrl(serviceId));

        StringAssert.Matches(job, new Regex(@"data-job-block>[\s\S]*?Vaga de emprego</h2>\s*<ul[^>]*>\s*<li>[^<]+</li>\s*<li>[^<]+</li>"), "as duas áreas continuam no bloco da vaga");
        Assert.IsFalse(job.Contains("(renomeado)", StringComparison.Ordinal), "o campo de áreas não se repete nas características");
        StringAssert.Contains(Text(service), Text(FieldLists.ServiceType.Find(1).Label), "o tipo do serviço continua no lugar do preço");
    }

    private sealed class RelabelledSpecsReader : IAdSpecsReader
    {
        public Task<IReadOnlyList<AdSpec>> ReadAsync(Ad ad, int categoryId, FieldGroup group, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AdSpec>>(AdAttributes.TryParse(ad.Attributes, out AdAttributes attributes)
                ? [.. AdSpecs.Build(attributes, group, categoryId).Select(spec => spec with { Label = spec.Label + " (renomeado)" })]
                : []);
    }

    [TestMethod]
    public async Task PreVisualizar_Servicos_MostraOTipoNoLugarDoPreco()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddInReviewAsync(site, Ana, "Diarista", 66, Day(29), ad =>
        {
            ad.SetPrice(null);
            ad.SetAttributes(new AdAttributes().Set("serviceTypeId", 1));
        });

        string html = await site.Admin.GetStringAsync(PreviewUrl(adId));

        StringAssert.Contains(html, "data-ad-value=\"tipo\"");
        StringAssert.Contains(Text(html), FieldLists.ServiceType.Find(1).Label, "o tipo do serviço aparece no lugar do preço");
        Assert.IsFalse(html.Contains("R$", StringComparison.Ordinal), "Serviços não têm preço");
    }

    [TestMethod]
    public async Task PreVisualizar_AnuncioForaDeRevisao_SoInforma_SemBotoesNemFaixa()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int published = await site.AddAdAsync(Ana, AdStatus.Published, "Já publicado", 33);
        int draft = await site.AddAdAsync(Ana, AdStatus.Draft, "Ainda rascunho", 33);

        foreach ((int id, string status) in new[] { (published, "Publicado"), (draft, "Rascunho") })
        {
            string html = await site.Admin.GetStringAsync(PreviewUrl(id));

            StringAssert.Contains(Text(html), "Este anúncio não está em revisão. Situação: " + status);
            Assert.IsFalse(html.Contains("data-preview-banner", StringComparison.Ordinal));
            Assert.IsFalse(html.Contains("data-preview-actions", StringComparison.Ordinal), "sem botões");
            Assert.IsFalse(Text(html).Contains("Editar", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public async Task PreVisualizar_AnuncioQueNaoExiste_E404()
    {
        using DraftSite site = await DraftSite.StartAsync();

        HttpResponseMessage response = await site.Admin.GetAsync(PreviewUrl(9999));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task PreVisualizar_TextoDoUsuarioSaiCodificado()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddInReviewAsync(site, Ana, "<img src=x onerror=alert(1)>", 33, Day(29), ad => ad.SetText("<img src=x onerror=alert(1)>", "<b>negrito</b>\n<script>alert(2)</script>"));

        string html = await site.Admin.GetStringAsync(PreviewUrl(adId));

        Assert.IsFalse(html.Contains("<img src=x", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains("<script>alert", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<b>negrito", StringComparison.Ordinal));
        StringAssert.Contains(html, "&lt;b&gt;negrito&lt;/b&gt;");
    }

    private sealed class FailingQueue : IReviewQueue
    {
        public Task<IReadOnlyList<ReviewQueueItem>> ListAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("segredo-do-banco");
    }
}

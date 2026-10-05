using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Photos;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Review;

/// <summary>Despublicar e arquivar (US-011-S01, S02, S03, S05, S06 e S07) pelas páginas do painel: situação no banco, tela do anúncio, foto entregue ao visitante, auditoria e permissões.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class TakedownTests
#pragma warning restore CA1515
{
    // O token antiforgery vale para a sessão toda; uma página que sempre abre (e que o Redator também vê) serve de fonte
    private const string TokenPage = "/painel/anuncios/novo";

    private static string Unpublish(int adId) => $"/painel/anuncios/{adId}/despublicar";

    private static string Archive(int adId) => $"/painel/anuncios/{adId}/arquivar";

    private static string EditUrl(int adId) => $"/painel/anuncios/{adId}/editar";

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path) => DraftSite.PostAsync(client, TokenPage, path);

    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<[^>]+>", " "), @"\s+", " ")).Trim();

    private static async Task<string> PageAsync(HttpClient client, string path) => await DraftSite.BodyAsync(await client.GetAsync(path));

    private static bool HasAction(string html, string path, string label) =>
        Regex.IsMatch(html, @"<a [^>]*href=""" + Regex.Escape(path) + @"""[^>]*>" + label + "</a>");

    private static async Task<List<AuditEntry>> AuditsAsync(DraftSite site, string action, int adId) =>
        [.. (await site.AuditAsync()).Where(e => e.Action == action && e.TargetId == adId.ToString(System.Globalization.CultureInfo.InvariantCulture))];

    private static async Task<HttpClient> SecondAdminAsync(DraftSite site)
    {
        await site.Harness.Factory.CreateUserAsync(SecondAdmin, "Carla Admin", PanelFixture.Password, RoleNames.Administrator);
        return await PanelFixture.SignedInAsync(site.Harness.Factory, SecondAdmin);
    }

    [TestMethod]
    public async Task US011S01_Despublicar_ConfirmaVoltaARascunho_LimpaAPublicacao_EAFotoSaiDoAr()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        DraftSite site = photos.Site;
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Ana, AdStatus.Published);
        using HttpClient visitor = site.Harness.Anonymous();
        string photoUrl = PhotoSite.Url(adId, photoId);
        Assert.AreEqual(HttpStatusCode.OK, (await visitor.GetAsync(photoUrl)).StatusCode, "publicado: a foto é pública");

        string confirmation = await PageAsync(site.Admin, Unpublish(adId));
        HttpResponseMessage done = await PostAsync(site.Admin, Unpublish(adId));

        StringAssert.Contains(Text(confirmation), "Despublicar este anúncio? Honda Civic 2018 Ele sai do site agora e volta a Rascunho. Você poderá corrigi-lo e enviá-lo de novo para revisão.");
        Assert.AreEqual(HttpStatusCode.Redirect, done.StatusCode);
        Assert.AreEqual(EditUrl(adId), done.Destination());
        Ad ad = await site.LoadAsync(adId);
        Assert.AreEqual(AdStatus.Draft, ad.Status);
        Assert.IsNull(ad.PublishedAt);
        Assert.IsNull(ad.PublishedById);
        string edit = await PageAsync(site.Admin, EditUrl(adId));
        StringAssert.Contains(Text(edit), "Anúncio despublicado");
        StringAssert.Contains(Text(edit), "Situação: Rascunho");
        Assert.IsFalse(Text(await PageAsync(site.Admin, EditUrl(adId))).Contains("Anúncio despublicado", StringComparison.Ordinal), "o aviso aparece uma vez só");
        Assert.AreEqual(HttpStatusCode.NotFound, (await visitor.GetAsync(photoUrl)).StatusCode, "despublicado: a foto sai do ar");
        List<AuditEntry> audits = await AuditsAsync(site, "ad.unpublish", adId);
        Assert.HasCount(1, audits);
        Assert.AreEqual(await site.UserIdAsync(PanelFixture.AdminEmail), audits[0].ActorId);
        Assert.AreEqual("Publicado", audits[0].PreviousValue);
        Assert.AreEqual("Rascunho", audits[0].NewValue);
    }

    [TestMethod]
    public async Task Despublicar_OAutorCorrigeEReenviaParaRevisao()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, "11912345678");
        int adId = await AddPublishableAsync(site);
        await DraftSite.PostAsync(site.Admin, TokenPage, $"/painel/anuncios/{adId}/publicar");
        Assert.AreEqual(AdStatus.Published, (await site.LoadAsync(adId)).Status);

        await PostAsync(site.Admin, Unpublish(adId));
        string authorPage = await PageAsync(site.Writer, EditUrl(adId));
        HttpResponseMessage sent = await PostAsync(site.Writer, $"/painel/anuncios/{adId}/enviar/confirmar");

        StringAssert.Contains(authorPage, "Salvar rascunho");
        Assert.IsFalse(authorPage.Contains("data-read-only", StringComparison.Ordinal), "o autor volta a poder editar");
        Assert.AreEqual(HttpStatusCode.Redirect, sent.StatusCode);
        Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(adId)).Status);
    }

    [TestMethod]
    public async Task US011S02_Arquivar_AvisoDefinitivo_SituacaoArquivado_EAFotoSaiDoAr()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        DraftSite site = photos.Site;
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Ana, AdStatus.Published);
        using HttpClient visitor = site.Harness.Anonymous();
        string photoUrl = PhotoSite.Url(adId, photoId);

        string confirmation = await PageAsync(site.Admin, Archive(adId));
        HttpResponseMessage done = await PostAsync(site.Admin, Archive(adId));

        StringAssert.Contains(Text(confirmation), "Arquivar este anúncio? Honda Civic 2018 O anúncio sairá do site e não poderá ser reativado.");
        Assert.AreEqual(HttpStatusCode.Redirect, done.StatusCode);
        Assert.AreEqual("/painel/anuncios", done.Destination());
        Ad ad = await site.LoadAsync(adId);
        Assert.AreEqual(AdStatus.Archived, ad.Status);
        Assert.IsNotNull(ad.ArchivedAt);
        StringAssert.Contains(Text(await PageAsync(site.Admin, "/painel/anuncios")), "Anúncio arquivado");
        Assert.AreEqual(HttpStatusCode.NotFound, (await visitor.GetAsync(photoUrl)).StatusCode, "arquivado: a foto sai do ar");
        List<AuditEntry> audits = await AuditsAsync(site, "ad.archive", adId);
        Assert.HasCount(1, audits);
        Assert.AreEqual(await site.UserIdAsync(PanelFixture.AdminEmail), audits[0].ActorId);
        Assert.AreEqual("Publicado", audits[0].PreviousValue);
        Assert.AreEqual("Arquivado", audits[0].NewValue);
    }

    [TestMethod]
    public async Task US011S03_Cancelar_AbrirAConfirmacaoNaoMudaNada_ECancelarVoltaParaOAnuncio()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        DraftSite site = photos.Site;
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Ana, AdStatus.Published);
        using HttpClient visitor = site.Harness.Anonymous();

        string unpublishPage = await PageAsync(site.Admin, Unpublish(adId));
        string archivePage = await PageAsync(site.Admin, Archive(adId));

        foreach (string page in new[] { unpublishPage, archivePage })
        {
            // "Cancelar" vem antes do botão de confirmar e leva o foco inicial (ação segura); é um link de volta, sem efeito
            StringAssert.Matches(page, new Regex(@"<a [^>]*href=""" + EditUrl(adId) + @"""[^>]*data-foco-inicial[^>]*>Cancelar</a>\s*<button [^>]*>(Despublicar|Arquivar)</button>"));
        }

        HttpResponseMessage cancelled = await site.Admin.GetAsync(EditUrl(adId));
        Assert.AreEqual(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.AreEqual(AdStatus.Published, (await site.LoadAsync(adId)).Status);
        Assert.AreEqual(HttpStatusCode.OK, (await visitor.GetAsync(PhotoSite.Url(adId, photoId))).StatusCode, "o anúncio continua no site");
        Assert.IsEmpty((await site.AuditAsync()).Where(e => e.Action is "ad.unpublish" or "ad.archive").ToList());
    }

    [TestMethod]
    [DataRow(AdStatus.Draft, "Rascunho")]
    [DataRow(AdStatus.InReview, "Em revisão")]
    [DataRow(AdStatus.Rejected, "Rejeitado")]
    public async Task US011S05_Arquivar_AnuncioQueAindaNaoFoiPublicado_VaiParaArquivado(byte status, string label)
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await site.AddAdAsync(Ana, status, "Moto para retirar peças");

        HttpResponseMessage done = await PostAsync(site.Admin, Archive(adId));

        Assert.AreEqual(HttpStatusCode.Redirect, done.StatusCode);
        Assert.AreEqual(AdStatus.Archived, (await site.LoadAsync(adId)).Status);
        List<AuditEntry> audits = await AuditsAsync(site, "ad.archive", adId);
        Assert.HasCount(1, audits);
        Assert.AreEqual(label, audits[0].PreviousValue);
        Assert.AreEqual("Arquivado", audits[0].NewValue);
        if (status == AdStatus.InReview)
        {
            Assert.IsFalse((await PageAsync(site.Admin, "/painel/anuncios/fila")).Contains("Moto para retirar peças", StringComparison.Ordinal), "sai da fila de revisão");
        }
    }

    [TestMethod]
    public async Task US011S06_AnuncioArquivado_SemAcoesDeRetirada_SomenteLeitura()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await site.AddAdAsync(Ana, AdStatus.Archived);

        string page = await PageAsync(site.Admin, EditUrl(adId));
        HttpResponseMessage unpublish = await PostAsync(site.Admin, Unpublish(adId));
        HttpResponseMessage archive = await PostAsync(site.Admin, Archive(adId));
        HttpResponseMessage getUnpublish = await site.Admin.GetAsync(Unpublish(adId));
        HttpResponseMessage getArchive = await site.Admin.GetAsync(Archive(adId));

        StringAssert.Contains(Text(page), "Situação: Arquivado");
        StringAssert.Contains(page, "data-read-only");
        Assert.IsFalse(page.Contains("data-takedown-actions", StringComparison.Ordinal));
        foreach (string absent in new[] { "Despublicar", "Arquivar" })
        {
            Assert.IsFalse(Text(page).Contains(absent, StringComparison.Ordinal), absent + " não aparece no anúncio arquivado");
        }

        foreach (HttpResponseMessage denied in new[] { unpublish, archive, getUnpublish, getArchive })
        {
            Assert.AreEqual(HttpStatusCode.Redirect, denied.StatusCode);
            Assert.AreEqual(EditUrl(adId), denied.Destination());
        }

        // O aviso é de uma leitura só: cada pedido é conferido logo depois de feito
        await PostAsync(site.Admin, Unpublish(adId));
        StringAssert.Contains(Text(await PageAsync(site.Admin, EditUrl(adId))), "Este anúncio não está mais publicado. Situação: Arquivado");
        await PostAsync(site.Admin, Archive(adId));
        StringAssert.Contains(Text(await PageAsync(site.Admin, EditUrl(adId))), "Este anúncio já foi arquivado");
        Assert.AreEqual(AdStatus.Archived, (await site.LoadAsync(adId)).Status);
        Assert.IsEmpty((await site.AuditAsync()).Where(e => e.Action is "ad.unpublish" or "ad.archive").ToList());
    }

    [TestMethod]
    [DataRow(AdStatus.Draft)]
    [DataRow(AdStatus.Rejected)]
    [DataRow(AdStatus.Published)]
    public async Task US011S07_Redator_NaoVeAsAcoesDeRetirada_NemFazPelaUrl(byte status)
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await site.AddAdAsync(Ana, status);
        string token = Regex.Match(await site.Writer.GetStringAsync(TokenPage), @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        string page = await PageAsync(site.Writer, EditUrl(adId));

        Assert.IsFalse(page.Contains("data-takedown-actions", StringComparison.Ordinal));
        foreach (string absent in new[] { "Despublicar", "Arquivar" })
        {
            Assert.IsFalse(Text(page).Contains(absent, StringComparison.Ordinal), absent + " não aparece para o Redator");
        }

        foreach (string path in new[] { Unpublish(adId), Archive(adId) })
        {
            HttpResponseMessage get = await site.Writer.GetAsync(path);
            using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
            HttpResponseMessage post = await site.Writer.PostAsync(path, form);
            foreach (HttpResponseMessage denied in new[] { get, post })
            {
                Assert.AreEqual(HttpStatusCode.Redirect, denied.StatusCode, path);
                StringAssert.StartsWith(denied.Destination(), "/painel/acesso-negado", path);
            }
        }

        Assert.AreEqual(status, (await site.LoadAsync(adId)).Status);
        Assert.IsEmpty((await site.AuditAsync()).Where(e => e.Action is "ad.unpublish" or "ad.archive").ToList());
    }

    [TestMethod]
    public async Task BarraDeAcoes_PorSituacao_AdministradorVeSoOQueOApendiceAPermite()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int draft = await site.AddAdAsync(Ana, AdStatus.Draft, "Rascunho A");
        int rejected = await site.AddAdAsync(Ana, AdStatus.Rejected, "Rejeitado A");
        int inReview = await site.AddAdAsync(Ana, AdStatus.InReview, "Revisão A");
        int published = await site.AddAdAsync(Ana, AdStatus.Published, "Publicado A");

        foreach (int id in new[] { draft, rejected, inReview })
        {
            string page = await PageAsync(site.Admin, EditUrl(id));
            Assert.IsTrue(HasAction(page, Archive(id), "Arquivar"), $"{id}: Arquivar na edição");
            Assert.IsFalse(HasAction(page, Unpublish(id), "Despublicar"), $"{id}: Despublicar só vale para publicado");
        }

        string publishedPage = await PageAsync(site.Admin, EditUrl(published));
        Assert.IsTrue(HasAction(publishedPage, Unpublish(published), "Despublicar"));
        Assert.IsTrue(HasAction(publishedPage, Archive(published), "Arquivar"));
        Assert.IsLessThan(publishedPage.IndexOf("Arquivar</a>", StringComparison.Ordinal), publishedPage.IndexOf("Despublicar</a>", StringComparison.Ordinal), "Arquivar por último");
        Assert.IsTrue(HasAction(await PageAsync(site.Admin, PreviewUrl(inReview)), Archive(inReview), "Arquivar"), "Em revisão: Arquivar na pré-visualização");
        Assert.IsFalse((await PageAsync(site.Admin, PreviewUrl(published))).Contains("Arquivar", StringComparison.Ordinal), "fora de revisão a pré-visualização só informa");
        Assert.IsFalse((await PageAsync(site.Admin, TokenPage)).Contains("data-takedown-actions", StringComparison.Ordinal), "anúncio novo não tem barra");
    }

    [TestMethod]
    [DataRow(AdStatus.Draft, "Rascunho")]
    [DataRow(AdStatus.InReview, "Em revisão")]
    [DataRow(AdStatus.Rejected, "Rejeitado")]
    [DataRow(AdStatus.Archived, "Arquivado")]
    public async Task Despublicar_AnuncioQueNaoEstaPublicado_DaAMensagemDaSituacaoENadaMuda(byte status, string label)
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await site.AddAdAsync(Ana, status);

        HttpResponseMessage get = await site.Admin.GetAsync(Unpublish(adId));
        HttpResponseMessage post = await PostAsync(site.Admin, Unpublish(adId));

        foreach (HttpResponseMessage response in new[] { get, post })
        {
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual(EditUrl(adId), response.Destination());
        }

        StringAssert.Contains(Text(await PageAsync(site.Admin, EditUrl(adId))), $"Este anúncio não está mais publicado. Situação: {label}");
        Assert.AreEqual(status, (await site.LoadAsync(adId)).Status);
        Assert.IsEmpty(await AuditsAsync(site, "ad.unpublish", adId));
    }

    [TestMethod]
    public async Task CliqueDuplo_MesmoAdministrador_SegundaRespostaDizOQueJaAconteceu_ComUmaSoAuditoria()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int toUnpublish = await site.AddAdAsync(Ana, AdStatus.Published, "Para despublicar");
        int toArchive = await site.AddAdAsync(Ana, AdStatus.Published, "Para arquivar");

        Assert.AreEqual(EditUrl(toUnpublish), (await PostAsync(site.Admin, Unpublish(toUnpublish))).Destination());
        await site.Admin.GetAsync(EditUrl(toUnpublish)); // lê o aviso de sucesso
        Assert.AreEqual(EditUrl(toUnpublish), (await PostAsync(site.Admin, Unpublish(toUnpublish))).Destination());
        StringAssert.Contains(Text(await PageAsync(site.Admin, EditUrl(toUnpublish))), "Este anúncio não está mais publicado. Situação: Rascunho");
        Assert.AreEqual("/painel/anuncios", (await PostAsync(site.Admin, Archive(toArchive))).Destination());
        await site.Admin.GetAsync("/painel/anuncios");
        Assert.AreEqual(EditUrl(toArchive), (await PostAsync(site.Admin, Archive(toArchive))).Destination());
        StringAssert.Contains(Text(await PageAsync(site.Admin, EditUrl(toArchive))), "Este anúncio já foi arquivado");
        Assert.HasCount(1, await AuditsAsync(site, "ad.unpublish", toUnpublish));
        Assert.HasCount(1, await AuditsAsync(site, "ad.archive", toArchive));
    }

    [TestMethod]
    public async Task DoisAdministradores_QuemChegaDepoisVeOQueMudou_ENadaFicaGravadoDuasVezes()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient second = await SecondAdminAsync(site);
        int archived = await site.AddAdAsync(Ana, AdStatus.Published, "Arquivado pela primeira");
        int unpublished = await site.AddAdAsync(Ana, AdStatus.Published, "Despublicado pela primeira");
        // A segunda pessoa abriu as duas confirmações enquanto os anúncios ainda estavam publicados
        string staleUnpublish = await second.GetStringAsync(Unpublish(archived));
        string token = Regex.Match(staleUnpublish, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        async Task<HttpResponseMessage> StalePost(string path)
        {
            using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
            return await second.PostAsync(path, form);
        }

        await PostAsync(site.Admin, Archive(archived));
        await PostAsync(site.Admin, Unpublish(unpublished));
        HttpResponseMessage lateUnpublish = await StalePost(Unpublish(archived));
        HttpResponseMessage lateArchive = await StalePost(Archive(unpublished));

        Assert.AreEqual(EditUrl(archived), lateUnpublish.Destination());
        StringAssert.Contains(Text(await PageAsync(second, EditUrl(archived))), "Este anúncio não está mais publicado. Situação: Arquivado");
        // Arquivar vale a partir de Rascunho: quem chega depois de um despublicar ainda consegue arquivar
        Assert.AreEqual("/painel/anuncios", lateArchive.Destination());
        Assert.AreEqual(AdStatus.Archived, (await site.LoadAsync(unpublished)).Status);
        Assert.AreEqual(AdStatus.Archived, (await site.LoadAsync(archived)).Status);
        Assert.HasCount(1, await AuditsAsync(site, "ad.archive", archived));
        Assert.HasCount(1, await AuditsAsync(site, "ad.unpublish", unpublished));
        Assert.HasCount(1, await AuditsAsync(site, "ad.archive", unpublished));
    }

    [TestMethod]
    public async Task SemLogin_SemToken_EAnuncioInexistente_SaoRecusados()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await site.AddAdAsync(Ana, AdStatus.Published);
        using HttpClient anonymous = site.Harness.Anonymous();

        foreach (string path in new[] { Unpublish(adId), Archive(adId) })
        {
            Assert.AreEqual(HttpStatusCode.Redirect, (await anonymous.GetAsync(path)).StatusCode, path);
            StringAssert.StartsWith((await anonymous.GetAsync(path)).Destination(), "/painel/entrar", path);
            using FormUrlEncodedContent noToken = new(new Dictionary<string, string>());
            Assert.AreEqual(HttpStatusCode.BadRequest, (await site.Admin.PostAsync(path, noToken)).StatusCode, "sem o token antiforgery: 400");
        }

        foreach (string path in new[] { Unpublish(9999), Archive(9999) })
        {
            Assert.AreEqual(HttpStatusCode.NotFound, (await site.Admin.GetAsync(path)).StatusCode, path);
            Assert.AreEqual(HttpStatusCode.NotFound, (await PostAsync(site.Admin, path)).StatusCode, path);
        }

        Assert.AreEqual(AdStatus.Published, (await site.LoadAsync(adId)).Status);
    }

    [TestMethod]
    public async Task Confirmacao_TituloComHtml_SaiCodificado()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await site.AddAdAsync(Ana, AdStatus.Published, "<script>alert(1)</script> Civic");

        string raw = await site.Admin.GetStringAsync(Archive(adId));

        Assert.IsFalse(raw.Contains("<script>alert(1)", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(raw, "&lt;script&gt;alert(1)&lt;/script&gt; Civic");
    }
}

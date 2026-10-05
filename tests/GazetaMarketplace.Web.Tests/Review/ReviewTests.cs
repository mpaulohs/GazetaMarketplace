using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Photos;
using GazetaMarketplace.Web.Tests.Support;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Review;

/// <summary>Publicar e rejeitar (US-010-S02 a S05, S07 e S08; tarefa 4.2) de ponta a ponta no site de teste: as páginas, a decisão, a trilha, a auditoria e as recusas.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ReviewTests
#pragma warning restore CA1515
{
    private const string Phone = "11912345678";
    private const string Reason = "Fotos escuras; envie fotos com boa iluminação";

    private static string Publish(int adId) => $"/painel/anuncios/{adId}/publicar";

    private static string RejectUrl(int adId) => $"/painel/anuncios/{adId}/rejeitar";

    // O token antiforgery vale para a sessão toda; uma página que sempre abre (e que o Redator também vê) serve de fonte, mesmo quando o anúncio já saiu de "Em revisão"
    private const string TokenPage = "/painel/anuncios/novo";

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, int adId) =>
        DraftSite.PostAsync(client, TokenPage, Publish(adId));

    private static Task<HttpResponseMessage> RejectAsync(HttpClient client, int adId, string reason) =>
        DraftSite.PostAsync(client, TokenPage, RejectUrl(adId), ("reason", reason));

    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<[^>]+>", " "), @"\s+", " ")).Trim();

    private static async Task<List<AuditEntry>> AuditsAsync(DraftSite site, string action, int adId) =>
        [.. (await site.AuditAsync()).Where(e => e.Action == action && e.TargetId == adId.ToString(System.Globalization.CultureInfo.InvariantCulture))];

    private static async Task<HttpClient> SecondAdminAsync(DraftSite site)
    {
        await site.Harness.Factory.CreateUserAsync(SecondAdmin, "Carla Admin", PanelFixture.Password, RoleNames.Administrator);
        return await PanelFixture.SignedInAsync(site.Harness.Factory, SecondAdmin);
    }

    [TestMethod]
    public async Task US010S03_Publicar_ConfirmaMudaASituacaoGravaAutorEDataEAnuncioSaiDaFila()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        int adId = await AddPublishableAsync(site);
        int other = await AddPublishableAsync(site, "Outro livro");

        string confirmation = await DraftSite.BodyAsync(await site.Admin.GetAsync(Publish(adId)));
        HttpResponseMessage published = await PublishAsync(site.Admin, adId);

        StringAssert.Contains(Text(confirmation), "Publicar este anúncio? Livro de Direito Civil Ele passa a aparecer no site para todos os visitantes.");
        StringAssert.Matches(confirmation, new Regex(@"<form method=""post""[^>]*action=""/painel/anuncios/" + adId + @"/publicar""[\s\S]*?<button [^>]*>Publicar</button>"));
        Assert.AreEqual(HttpStatusCode.Redirect, published.StatusCode);
        Assert.AreEqual("/painel/anuncios/fila", published.Destination());
        Ad ad = await site.LoadAsync(adId);
        Assert.AreEqual(AdStatus.Published, ad.Status);
        Assert.IsNotNull(ad.PublishedAt);
        Assert.AreEqual(await site.UserIdAsync(PanelFixture.AdminEmail), ad.PublishedById, "quem publicou fica gravado");
        Assert.IsNull(ad.RejectedAt);
        string queue = await DraftSite.BodyAsync(await site.Admin.GetAsync("/painel/anuncios/fila"));
        StringAssert.Contains(Text(queue), "Anúncio publicado");
        Assert.IsFalse(queue.Contains("Livro de Direito Civil", StringComparison.Ordinal), "sai da fila");
        StringAssert.Contains(queue, "Outro livro");
        Assert.IsFalse(Text(await DraftSite.BodyAsync(await site.Admin.GetAsync("/painel/anuncios/fila"))).Contains("Anúncio publicado", StringComparison.Ordinal), "o aviso aparece uma vez só");
        Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(other)).Status, "só o anúncio decidido muda");
    }

    [TestMethod]
    public async Task Publicar_RegistraUmaAuditoria_ComAtorEAsSituacoes()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        int adId = await AddPublishableAsync(site);

        await PublishAsync(site.Admin, adId);

        List<AuditEntry> audits = await AuditsAsync(site, "ad.publish", adId);
        Assert.HasCount(1, audits);
        Assert.AreEqual(await site.UserIdAsync(PanelFixture.AdminEmail), audits[0].ActorId);
        Assert.AreEqual("Em revisão", audits[0].PreviousValue);
        Assert.AreEqual("Publicado", audits[0].NewValue);
        Assert.AreEqual(AuditResult.Success, audits[0].Result);
    }

    [TestMethod]
    public async Task Publicar_FotoDoAnuncioPassaAChegarAoVisitanteSemLogin()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        DraftSite site = photos.Site;
        await SetPhoneAsync(site, Phone);
        int adId = await AddPublishableAsync(site, photos: 0);
        int photoId = await AddRealPhotoAsync(photos, adId);
        using HttpClient visitor = site.Harness.Anonymous();
        string url = PhotoSite.Url(adId, photoId);

        Assert.AreEqual(HttpStatusCode.NotFound, (await visitor.GetAsync(url)).StatusCode, "em revisão: o visitante não vê");
        await PublishAsync(site.Admin, adId);
        HttpResponseMessage afterPublish = await visitor.GetAsync(url);

        Assert.AreEqual(HttpStatusCode.OK, afterPublish.StatusCode, "publicado: a foto é pública");
        StringAssert.Contains(afterPublish.Headers.CacheControl!.ToString(), "public");
    }

    [TestMethod]
    public async Task US010S04_Rejeitar_ComMotivo_MudaASituacao_AutorVeOMotivo_ENaoFicaPublico()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        DraftSite site = photos.Site;
        int adId = await AddPublishableAsync(site, photos: 0);
        int photoId = await AddRealPhotoAsync(photos, adId);
        using HttpClient visitor = site.Harness.Anonymous();
        string form = await DraftSite.BodyAsync(await site.Admin.GetAsync(RejectUrl(adId)));

        HttpResponseMessage rejected = await RejectAsync(site.Admin, adId, "  " + Reason + "  ");

        StringAssert.Contains(Text(form), "Rejeitar anúncio");
        StringAssert.Matches(form, new Regex(@"<label [^>]*for=""motivo"">Motivo da rejeição"));
        Assert.AreEqual(HttpStatusCode.Redirect, rejected.StatusCode);
        Assert.AreEqual("/painel/anuncios/fila", rejected.Destination());
        Ad ad = await site.LoadAsync(adId);
        Assert.AreEqual(AdStatus.Rejected, ad.Status);
        Assert.AreEqual(Reason, ad.RejectionReason, "o motivo é gravado sem os espaços das pontas");
        Assert.AreEqual(await site.UserIdAsync(PanelFixture.AdminEmail), ad.RejectedById);
        Assert.IsNotNull(ad.RejectedAt);
        Assert.IsNull(ad.PublishedAt);
        string queue = await DraftSite.BodyAsync(await site.Admin.GetAsync("/painel/anuncios/fila"));
        StringAssert.Contains(Text(queue), "Anúncio rejeitado");
        Assert.IsFalse(queue.Contains("Livro de Direito Civil", StringComparison.Ordinal));
        // O autor (Ana) vê o motivo ao abrir o anúncio; o visitante continua sem ver a foto
        string authorPage = await DraftSite.BodyAsync(await site.Writer.GetAsync($"/painel/anuncios/{adId}/editar"));
        StringAssert.Contains(Text(authorPage), "Este anúncio foi rejeitado. Motivo: " + Reason);
        Assert.AreEqual(HttpStatusCode.NotFound, (await visitor.GetAsync(PhotoSite.Url(adId, photoId))).StatusCode);
    }

    [TestMethod]
    public async Task Rejeitar_AuditoriaGuardaOMotivo_ECadaDecisaoTemUmaSoAuditoria()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddPublishableAsync(site);

        await RejectAsync(site.Admin, adId, Reason);

        List<AuditEntry> audits = await AuditsAsync(site, "ad.reject", adId);
        Assert.HasCount(1, audits);
        Assert.AreEqual(await site.UserIdAsync(PanelFixture.AdminEmail), audits[0].ActorId);
        Assert.AreEqual("Em revisão", audits[0].PreviousValue);
        Assert.AreEqual("Rejeitado — motivo: " + Reason, audits[0].NewValue, "o histórico do motivo fica na auditoria");
        Assert.IsEmpty(await AuditsAsync(site, "ad.publish", adId));
    }

    [TestMethod]
    public async Task Rejeitar_ReenvioLimpaOMotivoDoAnuncio_MasAAuditoriaGuardaOHistorico()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddPublishableAsync(site);
        await RejectAsync(site.Admin, adId, Reason);

        HttpResponseMessage resubmitted = await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", $"/painel/anuncios/{adId}/enviar/confirmar");

        Assert.AreEqual(HttpStatusCode.Redirect, resubmitted.StatusCode);
        Ad ad = await site.LoadAsync(adId);
        Assert.AreEqual(AdStatus.InReview, ad.Status);
        Assert.IsNull(ad.RejectionReason, "o anúncio não guarda mais o motivo");
        Assert.AreEqual("Rejeitado — motivo: " + Reason, (await AuditsAsync(site, "ad.reject", adId)).Single().NewValue);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("\t\n ")]
    public async Task US010S05_RejeitarSemMotivo_MostraOErro_PreservaOQueFoiDigitado_ESituacaoNaoMuda(string reason)
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddPublishableAsync(site);

        HttpResponseMessage response = await RejectAsync(site.Admin, adId, reason);
        string html = await DraftSite.BodyAsync(response);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Matches(html, new Regex(@"<div class=""invalid-feedback d-block"" id=""erro-motivo"" role=""alert"">Informe o motivo da rejeição</div>"));
        StringAssert.Matches(html, new Regex(@"<textarea [^>]*aria-describedby=""ajuda-motivo erro-motivo""[^>]*aria-invalid=""true"""));
        Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(adId)).Status);
        Assert.IsEmpty(await AuditsAsync(site, "ad.reject", adId));
    }

    [TestMethod]
    public async Task Rejeitar_MotivoComExatamente500Caracteres_Passa_Com501Nao_ETextoVolta()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int accepted = await AddPublishableAsync(site, "Aceito");
        int refused = await AddPublishableAsync(site, "Recusado");
        string limit = new('a', 500);
        string tooLong = new string('b', 501);

        HttpResponseMessage ok = await RejectAsync(site.Admin, accepted, limit);
        HttpResponseMessage bad = await RejectAsync(site.Admin, refused, tooLong);
        string badHtml = await DraftSite.BodyAsync(bad);

        Assert.AreEqual(HttpStatusCode.Redirect, ok.StatusCode);
        Assert.AreEqual(limit, (await site.LoadAsync(accepted)).RejectionReason);
        StringAssert.Contains(badHtml, "O motivo pode ter no máximo 500 caracteres");
        StringAssert.Contains(badHtml, tooLong, "o texto digitado volta para a pessoa corrigir");
        Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(refused)).Status);
    }

    [TestMethod]
    public async Task Rejeitar_MotivoComHtml_SaiCodificadoNaPaginaDoAutor_ENoFormularioDeErro()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddPublishableAsync(site);
        const string Hostile = "<script>alert(1)</script> \"aspas\" & <b>negrito</b>";

        await RejectAsync(site.Admin, adId, Hostile);
        string authorRaw = await site.Writer.GetStringAsync($"/painel/anuncios/{adId}/editar");
        HttpResponseMessage again = await site.Admin.GetAsync(RejectUrl(adId));

        Assert.IsFalse(authorRaw.Contains("<script>alert(1)", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(authorRaw.Contains("<b>negrito", StringComparison.Ordinal));
        StringAssert.Contains(authorRaw, "&lt;script&gt;alert(1)&lt;/script&gt;");
        Assert.AreEqual(HttpStatusCode.Redirect, again.StatusCode, "anúncio já rejeitado não abre mais o formulário");
        StringAssert.Contains(again.Destination(), "/pre-visualizacao");
    }

    [TestMethod]
    public async Task US010S07_DoisAdministradores_QuemPerdeVeAMensagemDaSpec_ESituacaoContinuaPublicado()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        int adId = await AddPublishableAsync(site);
        using HttpClient second = await SecondAdminAsync(site);
        // Os dois abrem a página de rejeitar/publicar quando o anúncio ainda está Em revisão
        string stalePage = await second.GetStringAsync(RejectUrl(adId));
        string token = Regex.Match(stalePage, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        Assert.AreEqual(HttpStatusCode.Redirect, (await PublishAsync(site.Admin, adId)).StatusCode);

        using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["reason"] = "Fotos escuras", ["__RequestVerificationToken"] = token });
        HttpResponseMessage lost = await second.PostAsync(RejectUrl(adId), form);

        Assert.AreEqual(HttpStatusCode.Redirect, lost.StatusCode);
        Assert.AreEqual(PreviewUrl(adId), lost.Destination());
        string preview = await DraftSite.BodyAsync(await second.GetAsync(lost.Destination()));
        StringAssert.Matches(preview, new Regex(@"role=""alert""[^>]*data-review-conflict>Este anúncio já foi publicado por outro administrador</div>"));
        StringAssert.Contains(Text(preview), "Situação: Publicado");
        Assert.AreEqual(AdStatus.Published, (await site.LoadAsync(adId)).Status);
        Assert.IsNull((await site.LoadAsync(adId)).RejectedById);
        Assert.IsEmpty(await AuditsAsync(site, "ad.reject", adId), "a decisão que perdeu não deixa auditoria");
        Assert.HasCount(1, await AuditsAsync(site, "ad.publish", adId));
    }

    [TestMethod]
    public async Task DoisAdministradores_RejeitadoPrimeiro_QuemPublicaVeQueJaFoiRejeitadoPorOutro()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        int adId = await AddPublishableAsync(site);
        using HttpClient second = await SecondAdminAsync(site);
        string stalePage = await second.GetStringAsync(Publish(adId));
        string token = Regex.Match(stalePage, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        await RejectAsync(site.Admin, adId, Reason);

        using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        HttpResponseMessage lost = await second.PostAsync(Publish(adId), form);
        string preview = await DraftSite.BodyAsync(await second.GetAsync(lost.Destination()));

        StringAssert.Contains(preview, "Este anúncio já foi rejeitado por outro administrador");
        StringAssert.Contains(Text(preview), "Situação: Rejeitado");
        Assert.AreEqual(AdStatus.Rejected, (await site.LoadAsync(adId)).Status);
    }

    [TestMethod]
    public async Task MesmoAdministradorDuasVezes_SegundaRespostaDizJaFoiPublicado_SemPorOutroAdministrador()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        int adId = await AddPublishableAsync(site);
        string page = await site.Admin.GetStringAsync(Publish(adId));
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        using FormUrlEncodedContent first = new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        using FormUrlEncodedContent secondClick = new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });

        HttpResponseMessage one = await site.Admin.PostAsync(Publish(adId), first);
        HttpResponseMessage two = await site.Admin.PostAsync(Publish(adId), secondClick);
        string preview = await DraftSite.BodyAsync(await site.Admin.GetAsync(two.Destination()));

        Assert.AreEqual("/painel/anuncios/fila", one.Destination());
        Assert.AreEqual(PreviewUrl(adId), two.Destination());
        StringAssert.Contains(preview, ">Este anúncio já foi publicado</div>");
        Assert.IsFalse(preview.Contains("por outro administrador", StringComparison.Ordinal));
        Assert.HasCount(1, await AuditsAsync(site, "ad.publish", adId), "uma passagem, uma auditoria");
    }

    [TestMethod]
    public async Task US010S08_PublicarSemOTelefoneDoSite_AvisaComLinkParaConfiguracoes_ESituacaoNaoMuda()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adId = await AddPublishableAsync(site);

        HttpResponseMessage confirm = await site.Admin.GetAsync(Publish(adId));
        HttpResponseMessage post = await PublishAsync(site.Admin, adId);
        string preview = await DraftSite.BodyAsync(await site.Admin.GetAsync(post.Destination()));

        Assert.AreEqual(PreviewUrl(adId), confirm.Destination(), "nem a confirmação abre sem o telefone");
        Assert.AreEqual(PreviewUrl(adId), post.Destination());
        StringAssert.Matches(preview, new Regex(@"role=""alert""[^>]*data-review-phone>\s*<span>Configure o telefone/WhatsApp do site antes de publicar</span>\s*<a [^>]*href=""/painel/configuracoes""[^>]*data-foco-inicial>Configurações</a>"));
        Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(adId)).Status);
        Assert.IsEmpty(await AuditsAsync(site, "ad.publish", adId));
        StringAssert.Contains(Text(preview), "Pré-visualização — ainda não publicado");
    }

    [TestMethod]
    public async Task Publicar_ReconfereAsPendencias_AnuncioEditadoSemFotoOuSemPrecoNaoVaiAoAr()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        int noPhoto = await AddPublishableAsync(site, "Sem foto", photos: 0);
        int noPrice = await AddPublishableAsync(site, "Sem preço");
        await site.Harness.WithDbAsync(async db =>
        {
            Ad ad = db.Ads.Single(a => a.Id == noPrice);
            ad.SetPrice(null);
            await db.SaveChangesAsync();
            return 0;
        });

        foreach ((int id, string missing, string target) in new[] { (noPhoto, "Adicione ao menos 1 foto", "fotos"), (noPrice, "Informe o preço", "preco") })
        {
            HttpResponseMessage confirm = await site.Admin.GetAsync(Publish(id));
            HttpResponseMessage post = await PublishAsync(site.Admin, id);
            string preview = await DraftSite.BodyAsync(await site.Admin.GetAsync(post.Destination()));

            Assert.AreEqual(PreviewUrl(id), confirm.Destination());
            StringAssert.Contains(Text(preview), "Falta 1 item para publicar este anúncio: " + missing);
            StringAssert.Matches(preview, new Regex(@"<a href=""/painel/anuncios/" + id + @"/editar#" + target + @""">" + Regex.Escape(missing) + "</a>"));
            Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(id)).Status, "a situação continua Em revisão");
            Assert.IsEmpty(await AuditsAsync(site, "ad.publish", id));
        }
    }

    [TestMethod]
    public async Task Decidir_AnuncioQueNaoEstaEmRevisao_DaAMensagemDaSituacaoENadaMuda()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        (byte status, string message)[] cases =
        [
            (AdStatus.Draft, "Este anúncio não está mais em revisão"),
            (AdStatus.Published, "Este anúncio já foi publicado"),
            (AdStatus.Rejected, "Este anúncio já foi rejeitado"),
            (AdStatus.Archived, "Este anúncio foi arquivado")
        ];

        foreach ((byte status, string message) in cases)
        {
            // AddAdAsync grava a decisão em nome do Administrador do teste: é o mesmo que decide agora
            int adId = await site.AddAdAsync(Ana, status, "Fora de revisão " + status, 86);
            string seed = await DraftSite.BodyAsync(await site.Admin.GetAsync(PreviewUrl(adId)));
            string token = Regex.Match(await site.Admin.GetStringAsync("/painel/configuracoes"), @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
            foreach ((string path, Dictionary<string, string> body) in new[]
            {
                (Publish(adId), new Dictionary<string, string> { ["__RequestVerificationToken"] = token }),
                (RejectUrl(adId), new Dictionary<string, string> { ["reason"] = "Motivo", ["__RequestVerificationToken"] = token })
            })
            {
                using FormUrlEncodedContent form = new(body);
                HttpResponseMessage post = await site.Admin.PostAsync(path, form);
                string page = await DraftSite.BodyAsync(await site.Admin.GetAsync(post.Destination()));

                Assert.AreEqual(PreviewUrl(adId), post.Destination(), $"{status} {path}");
                StringAssert.Contains(page, ">" + message + "</div>", $"{status} {path}");
            }

            Assert.AreEqual(status, (await site.LoadAsync(adId)).Status, "a situação não muda");
            Assert.IsEmpty(await AuditsAsync(site, "ad.publish", adId));
            Assert.IsEmpty(await AuditsAsync(site, "ad.reject", adId));
            Assert.IsNotNull(seed);
        }
    }

    [TestMethod]
    public async Task Redator_NaoDecide_NemPeloGetNemPeloPost_ENadaMuda()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        int adId = await AddPublishableAsync(site);
        string token = Regex.Match(await site.Writer.GetStringAsync("/painel/anuncios/novo"), @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        foreach (string path in new[] { Publish(adId), RejectUrl(adId) })
        {
            HttpResponseMessage get = await site.Writer.GetAsync(path);
            using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["reason"] = "x", ["__RequestVerificationToken"] = token });
            HttpResponseMessage post = await site.Writer.PostAsync(path, form);

            foreach (HttpResponseMessage denied in new[] { get, post })
            {
                Assert.AreEqual(HttpStatusCode.Redirect, denied.StatusCode, path);
                StringAssert.StartsWith(denied.Destination(), "/painel/acesso-negado", path);
            }
        }

        Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(adId)).Status);
        Assert.IsEmpty(await AuditsAsync(site, "ad.publish", adId));
        Assert.IsEmpty(await AuditsAsync(site, "ad.reject", adId));
    }

    [TestMethod]
    public async Task SemLogin_SemTokenEAnuncioInexistente_SaoRecusados()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await SetPhoneAsync(site, Phone);
        int adId = await AddPublishableAsync(site);
        using HttpClient anonymous = site.Harness.Anonymous();

        foreach (string path in new[] { Publish(adId), RejectUrl(adId) })
        {
            Assert.AreEqual(HttpStatusCode.Redirect, (await anonymous.GetAsync(path)).StatusCode, path);
            StringAssert.StartsWith((await anonymous.GetAsync(path)).Destination(), "/painel/entrar", path);
            using FormUrlEncodedContent noToken = new(new Dictionary<string, string> { ["reason"] = "x" });
            Assert.AreEqual(HttpStatusCode.BadRequest, (await site.Admin.PostAsync(path, noToken)).StatusCode, "sem o token antiforgery: 400");
        }

        Assert.AreEqual(HttpStatusCode.NotFound, (await site.Admin.GetAsync(Publish(9999))).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await site.Admin.GetAsync(RejectUrl(9999))).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await PublishAsync(site.Admin, 9999)).StatusCode);
        Assert.AreEqual(AdStatus.InReview, (await site.LoadAsync(adId)).Status);
    }

    /// <summary>Uma foto de verdade (processada e gravada pelo site) numa linha de <c>AdPhotos</c> do anúncio.</summary>
    private static async Task<int> AddRealPhotoAsync(PhotoSite photos, int adId)
    {
        using IServiceScope scope = photos.Site.Harness.Factory.Services.CreateScope();
        IPhotoIngestion ingestion = scope.ServiceProvider.GetRequiredService<IPhotoIngestion>();
        StoredPhoto stored = await ingestion.IngestAsync(adId, new MemoryStream(PhotoFixtures.Solid(MagickFormat.Jpeg, 2000, 1000)), System.Threading.CancellationToken.None);
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        AdPhoto photo = new() { AdId = adId, SortOrder = 0, StorageKey = stored.StorageKey, OriginalKey = stored.OriginalKey, Width = stored.Width, Height = stored.Height, SizeBytes = stored.SizeBytes };
        db.AdPhotos.Add(photo);
        await db.SaveChangesAsync();
        return photo.Id;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Photos;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>Enviar o anúncio para revisão (US-009) de ponta a ponta no site de teste: o botão salva e confere, a página de confirmação, o envio, o clique duplo e o acesso.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SubmitForReviewTests
#pragma warning restore CA1515
{
    private const string Writer = PanelFixture.WriterEmail;

    private static readonly (string Name, string Value)[] Book =
    [
        ("Title", "Livro de Direito Civil"), ("Description", "Edição 2020, sem anotações"), ("CategoryId", "86"), ("Price", "50,00"),
        ("Cep", "13015-100"), ("Fields[conditionId]", "2")
    ];

    private static readonly (string Name, string Value)[] Car =
    [
        ("Title", "Honda Civic 2019"), ("Description", "Único dono"), ("CategoryId", "33"), ("Price", "62.000,00"), ("Cep", "13015-100"),
        ("Fields[brandId]", "1"), ("Fields[modelId]", "11"), ("Fields[modelYear]", "2019"), ("Fields[versionId]", "100"), ("Fields[km]", "45000")
    ];

    private static string Edit(int adId) => $"/painel/anuncios/{adId}/editar";

    private static (string Name, string Value)[] Without(IEnumerable<(string Name, string Value)> fields, params string[] names) =>
        [.. fields.Where(f => !names.Contains(f.Name))];

    private static (string Name, string Value)[] With(IEnumerable<(string Name, string Value)> fields, string name, string value) =>
        [.. fields.Where(f => f.Name != name).Append((name, value))];

    /// <summary>Cria o rascunho com os campos dados (e, se pedido, uma foto) e devolve o id.</summary>
    private static async Task<int> CreateAsync(PhotoSite photos, (string Name, string Value)[] fields, bool withPhoto = true)
    {
        int adId = DraftSite.IdFrom(await DraftSite.PostNewAsync(photos.Site.Writer, fields));
        if (withPhoto)
        {
            await photos.AddRowsAsync(adId, 1);
        }

        return adId;
    }

    /// <summary>O botão "Enviar para revisão": manda o formulário inteiro para <c>/enviar</c>.</summary>
    private static Task<HttpResponseMessage> PressSubmitAsync(HttpClient client, int adId, params (string Name, string Value)[] fields) =>
        DraftSite.PostAsync(client, "/painel/anuncios/novo", $"/painel/anuncios/{adId}/enviar", fields);

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, int adId) =>
        DraftSite.PostAsync(client, "/painel/anuncios/novo", $"/painel/anuncios/{adId}/enviar/confirmar");

    private static async Task<int> SubmitAuditsAsync(PhotoSite photos) =>
        (await photos.Site.AuditAsync()).Count(e => e.Action == "ad.submit");

    private static async Task<string> PageAsync(HttpClient client, string url) => await DraftSite.BodyAsync(await client.GetAsync(url));

    [TestMethod]
    public async Task US009S01_EnviarUmRascunhoCompleto_PedeConfirmacao_Envia_EMostraAMensagem()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await CreateAsync(photos, Book);

        HttpResponseMessage pressed = await PressSubmitAsync(photos.Site.Writer, adId, Book);

        Assert.AreEqual(HttpStatusCode.Redirect, pressed.StatusCode);
        Assert.AreEqual($"/painel/anuncios/{adId}/enviar/confirmar", pressed.Destination(), "sem pendências, vai à página de confirmação");
        Assert.AreEqual(AdStatus.Draft, (await photos.Site.LoadAsync(adId)).Status, "só pedir o envio não muda a situação");
        string confirmation = await PageAsync(photos.Site.Writer, pressed.Destination());
        StringAssert.Contains(confirmation, "Enviar para revisão?");
        StringAssert.Contains(confirmation, "Livro de Direito Civil");
        StringAssert.Contains(confirmation, "somente para leitura");

        HttpResponseMessage confirmed = await ConfirmAsync(photos.Site.Writer, adId);

        Assert.AreEqual(HttpStatusCode.Redirect, confirmed.StatusCode);
        Assert.AreEqual("/painel/anuncios", confirmed.Destination());
        StringAssert.Contains(await PageAsync(photos.Site.Writer, "/painel/anuncios"), "Anúncio enviado para revisão");
        Ad ad = await photos.Site.LoadAsync(adId);
        Assert.AreEqual(AdStatus.InReview, ad.Status);
        Assert.IsNotNull(ad.SentAt, "a data de envio é gravada");
        Assert.AreEqual(1, await SubmitAuditsAsync(photos), "uma auditoria ad.submit");
        string readOnly = await PageAsync(photos.Site.Writer, Edit(adId));
        StringAssert.Contains(readOnly, "Em revisão");
        StringAssert.Contains(readOnly, "data-read-only");
        Assert.IsFalse(readOnly.Contains("Enviar para revisão", StringComparison.Ordinal), "depois de enviado não há mais o botão");
    }

    [TestMethod]
    public async Task US009S02_RascunhoSemFotoESemCep_MostraAListaComLinks_ESituacaoContinuaRascunho()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (string Name, string Value)[] incomplete = Without(Book, "Cep");
        int adId = await CreateAsync(photos, incomplete, withPhoto: false);

        HttpResponseMessage pressed = await PressSubmitAsync(photos.Site.Writer, adId, incomplete);

        Assert.AreEqual(HttpStatusCode.Redirect, pressed.StatusCode);
        Assert.AreEqual(Edit(adId) + "?pendencias=true", pressed.Destination());
        string page = await PageAsync(photos.Site.Writer, pressed.Destination());
        StringAssert.Contains(page, "Faltam 2 itens para enviar este anúncio para revisão");
        Assert.IsTrue(Regex.IsMatch(page, @"<a href=""#cep""[^>]*>Informe o CEP</a>"), "o CEP tem link para o campo");
        Assert.IsTrue(Regex.IsMatch(page, @"<a href=""#fotos""[^>]*>Adicione ao menos 1 foto</a>"), "a foto tem link para a seção");
        Assert.IsTrue(Regex.IsMatch(page, @"role=""alert""[^>]*data-pendings"), "a lista é um alerta, que recebe o foco");
        StringAssert.Contains(page, "id=\"cep\"");
        StringAssert.Contains(page, "id=\"fotos\"");
        Assert.AreEqual(AdStatus.Draft, (await photos.Site.LoadAsync(adId)).Status);
        Assert.AreEqual(0, await SubmitAuditsAsync(photos));
    }

    [TestMethod]
    public async Task US009S03_FaltaAQuilometragem_MostraSoEssaPendencia_ComLinkParaOCampo()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (string Name, string Value)[] noKm = Without(Car, "Fields[km]");
        int adId = await CreateAsync(photos, noKm);

        HttpResponseMessage pressed = await PressSubmitAsync(photos.Site.Writer, adId, noKm);

        string page = await PageAsync(photos.Site.Writer, pressed.Destination());
        StringAssert.Contains(page, "Falta 1 item para enviar este anúncio para revisão");
        Assert.IsTrue(Regex.IsMatch(page, @"<a href=""#campo-km""[^>]*>Informe a quilometragem</a>"));
        StringAssert.Contains(page, "id=\"campo-km\"");
        Assert.AreEqual(AdStatus.Draft, (await photos.Site.LoadAsync(adId)).Status);
    }

    [TestMethod]
    public async Task US009S04_ReenviarUmAnuncioRejeitadoDepoisDeCorrigi_lo_VoltaParaEmRevisao_ELimpaOMotivo()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer, AdStatus.Rejected, categoryId: 86);
        await photos.AddRowsAsync(adId, 1);
        Assert.AreEqual("Fotos escuras", (await photos.Site.LoadAsync(adId)).RejectionReason, "ponto de partida: rejeitado com motivo");

        HttpResponseMessage pressed = await PressSubmitAsync(photos.Site.Writer, adId, Book); // a pessoa corrigiu o que faltava
        Assert.AreEqual($"/painel/anuncios/{adId}/enviar/confirmar", pressed.Destination());
        await ConfirmAsync(photos.Site.Writer, adId);

        Ad ad = await photos.Site.LoadAsync(adId);
        Assert.AreEqual(AdStatus.InReview, ad.Status);
        Assert.IsNull(ad.RejectionReason, "o motivo da rejeição some da tela (o histórico fica na auditoria)");
        Assert.IsNotNull(ad.SentAt);
        Assert.AreEqual(1, await SubmitAuditsAsync(photos));
    }

    [TestMethod]
    public async Task US009S05_CliqueDuploNaConfirmacao_UmaSoPassagem_UmaAuditoria_ASegundaVezDizJaFoiEnviado()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await CreateAsync(photos, Book);

        HttpResponseMessage first = await ConfirmAsync(photos.Site.Writer, adId);
        string firstPage = await PageAsync(photos.Site.Writer, first.Destination());
        HttpResponseMessage second = await ConfirmAsync(photos.Site.Writer, adId);
        string secondPage = await PageAsync(photos.Site.Writer, second.Destination());

        Assert.AreEqual(HttpStatusCode.Redirect, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.Redirect, second.StatusCode, "a segunda vez não é erro");
        StringAssert.Contains(firstPage, "Anúncio enviado para revisão");
        StringAssert.Contains(secondPage, "Este anúncio já foi enviado para revisão");
        Assert.AreEqual(AdStatus.InReview, (await photos.Site.LoadAsync(adId)).Status);
        Assert.AreEqual(1, await SubmitAuditsAsync(photos), "uma única entrada na fila e uma única auditoria");
    }

    [TestMethod]
    public async Task OBotaoSalvaOFormularioAntesDeConferir_MesmoQuandoHaPendencias()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (string Name, string Value)[] noCep = Without(Book, "Cep");
        int adId = await CreateAsync(photos, noCep, withPhoto: false);

        await PressSubmitAsync(photos.Site.Writer, adId, With(noCep, "Title", "Título corrigido na hora"));

        Assert.AreEqual("Título corrigido na hora", (await photos.Site.LoadAsync(adId)).Title, "o que foi digitado não se perde");
    }

    [TestMethod]
    public async Task FormularioInvalido_NaoEnvia_VoltaComOErroJuntoAoCampo()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await CreateAsync(photos, Book);

        HttpResponseMessage pressed = await PressSubmitAsync(photos.Site.Writer, adId, With(Book, "Title", ""));

        Assert.AreEqual(HttpStatusCode.OK, pressed.StatusCode);
        StringAssert.Contains(await DraftSite.BodyAsync(pressed), "Informe um título");
        Assert.AreEqual(AdStatus.Draft, (await photos.Site.LoadAsync(adId)).Status);
        Assert.AreEqual("Livro de Direito Civil", (await photos.Site.LoadAsync(adId)).Title, "nada foi salvo");
    }

    [TestMethod]
    public async Task OBotaoEnviarNuncaFicaDesabilitado_SoAparecePosOPrimeiroSalvar_ENaoNaLeitura()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int draft = await CreateAsync(photos, Without(Book, "Cep"), withPhoto: false); // cheio de pendências
        int inReview = await photos.Site.AddAdAsync(Writer, AdStatus.InReview);

        string newPage = await PageAsync(photos.Site.Writer, "/painel/anuncios/novo");
        string draftPage = await PageAsync(photos.Site.Writer, Edit(draft));
        string reviewPage = await PageAsync(photos.Site.Writer, Edit(inReview));

        Assert.IsFalse(newPage.Contains("Enviar para revisão", StringComparison.Ordinal), "anúncio novo ainda não tem id: o botão só aparece depois do primeiro Salvar rascunho");
        Match button = Regex.Match(draftPage, @"<button[^>]*>Enviar para revisão</button>");
        Assert.IsTrue(button.Success, "rascunho gravado tem o botão");
        Assert.IsFalse(button.Value.Contains("disabled", StringComparison.Ordinal), "e ele não é desabilitado por haver pendência");
        StringAssert.Contains(button.Value, $"formaction=\"/painel/anuncios/{draft}/enviar\"");
        StringAssert.Contains(button.Value, "form=\"anuncio-form\"");
        Assert.IsFalse(reviewPage.Contains("Enviar para revisão", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ConfirmacaoEPostDeConfirmacao_ConferemDeNovoNoServidor_ComPendenciaVoltamParaLista()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await CreateAsync(photos, Book, withPhoto: false); // sem foto

        HttpResponseMessage get = await photos.Site.Writer.GetAsync($"/painel/anuncios/{adId}/enviar/confirmar");
        HttpResponseMessage post = await ConfirmAsync(photos.Site.Writer, adId);

        Assert.AreEqual(Edit(adId) + "?pendencias=true", get.Destination());
        Assert.AreEqual(Edit(adId) + "?pendencias=true", post.Destination());
        Assert.AreEqual(AdStatus.Draft, (await photos.Site.LoadAsync(adId)).Status, "ir direto à confirmação não pula a conferência");
        Assert.AreEqual(0, await SubmitAuditsAsync(photos));
    }

    [TestMethod]
    public async Task Acesso_OutroRedatorEmRevisaoEPublicado_Recusam_OAdministradorEnvia_Inexistente404()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        await photos.Site.Harness.Factory.CreateUserAsync("outro.redator@exemplo.com.br", "Outro Redator", PanelFixture.Password, RoleNames.Writer);
        using HttpClient stranger = await PanelFixture.SignedInAsync(photos.Site.Harness.Factory, "outro.redator@exemplo.com.br");
        int draft = await CreateAsync(photos, Book);
        int inReview = await photos.Site.AddAdAsync(Writer, AdStatus.InReview);
        int published = await photos.Site.AddAdAsync(Writer, AdStatus.Published);

        HttpResponseMessage strangerGet = await stranger.GetAsync($"/painel/anuncios/{draft}/enviar/confirmar");
        HttpResponseMessage strangerPost = await DraftSite.PostAsync(stranger, "/painel/anuncios/novo", $"/painel/anuncios/{draft}/enviar/confirmar");
        HttpResponseMessage strangerPress = await DraftSite.PostAsync(stranger, "/painel/anuncios/novo", $"/painel/anuncios/{draft}/enviar", Book);
        HttpResponseMessage reviewGet = await photos.Site.Writer.GetAsync($"/painel/anuncios/{inReview}/enviar/confirmar");
        HttpResponseMessage publishedGet = await photos.Site.Writer.GetAsync($"/painel/anuncios/{published}/enviar/confirmar");
        HttpResponseMessage publishedPost = await ConfirmAsync(photos.Site.Writer, published);
        HttpResponseMessage missing = await photos.Site.Writer.GetAsync("/painel/anuncios/99999/enviar/confirmar");

        foreach (HttpResponseMessage denied in new[] { strangerGet, strangerPost, strangerPress, reviewGet, publishedGet, publishedPost })
        {
            Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.AreEqual(AdStatus.Draft, (await photos.Site.LoadAsync(draft)).Status, "o intruso não enviou");
        Assert.AreEqual(AdStatus.Published, (await photos.Site.LoadAsync(published)).Status);

        HttpResponseMessage adminConfirm = await ConfirmAsync(photos.Site.Admin, draft);
        Assert.AreEqual(HttpStatusCode.Redirect, adminConfirm.StatusCode);
        Assert.AreEqual(AdStatus.InReview, (await photos.Site.LoadAsync(draft)).Status, "o Administrador envia pelo mesmo caminho, sem atalho");
    }

    [TestMethod]
    public async Task AdministradorEnviaOProprioAnuncio_PeloMesmoCaminhoDoRedator()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = DraftSite.IdFrom(await DraftSite.PostNewAsync(photos.Site.Admin, Book));
        await photos.AddRowsAsync(adId, 1);

        HttpResponseMessage pressed = await PressSubmitAsync(photos.Site.Admin, adId, Book);
        await ConfirmAsync(photos.Site.Admin, adId);

        Assert.AreEqual($"/painel/anuncios/{adId}/enviar/confirmar", pressed.Destination());
        Assert.AreEqual(AdStatus.InReview, (await photos.Site.LoadAsync(adId)).Status, "vai a Em revisão, e não direto a Publicado");
    }
}

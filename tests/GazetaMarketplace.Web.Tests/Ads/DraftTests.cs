using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Catalog;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>Ajudantes das telas do anúncio: site de teste com o catálogo pequeno e as cidades de SP, e o envio do formulário com o token da página.</summary>
internal sealed class DraftSite : IDisposable
{
    private DraftSite(CepHarness harness) => Harness = harness;

    public CepHarness Harness { get; }

    public HttpClient Writer => Harness.Writer;

    public HttpClient Admin => Harness.Admin;

    public static async Task<DraftSite> StartAsync(int? requestsPerMinute = null)
    {
        CepHarness harness = await CepHarness.StartAsync(configuration: requestsPerMinute is { } limit
            ? new Dictionary<string, string> { ["RateLimiting:GlobalPerMinute"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            : null);
        await harness.WithDbAsync(async db =>
        {
            SmallCatalog.Seed(db);
            return await Task.FromResult(0);
        });
        await harness.AddCitiesAsync(
            new City { IbgeCode = 3509502, Name = "Campinas", Uf = "SP", NameSearch = "campinas" },
            new City { IbgeCode = 3550308, Name = "São Paulo", Uf = "SP", NameSearch = "sao paulo" });
        return new DraftSite(harness);
    }

    public async Task<int> UserIdAsync(string email) => (await Harness.Factory.ListUsersAsync()).Single(u => u.Email == email).Id;

    /// <summary>Grava um anúncio direto no banco, na situação pedida.</summary>
    public async Task<int> AddAdAsync(string authorEmail, byte status = AdStatus.Draft, string title = "Honda Civic 2018", int? categoryId = null, long? price = null)
    {
        int author = await UserIdAsync(authorEmail);
        int decider = await UserIdAsync(PanelFixture.AdminEmail);
        Ad ad = AdFactory.At(status, author, title, categoryId, decider);
        if (price is not null)
        {
            ad.SetPrice(price);
        }

        return await Harness.WithDbAsync(async db =>
        {
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            return ad.Id;
        });
    }

    public Task<Ad> LoadAsync(int id) => Harness.WithDbAsync(db => db.Ads.AsNoTracking().SingleAsync(a => a.Id == id));

    public Task<int> CountAsync() => Harness.WithDbAsync(db => db.Ads.CountAsync());

    public Task<List<AuditEntry>> AuditAsync() => Harness.WithDbAsync(db => db.AuditEntries.AsNoTracking().OrderBy(e => e.Id).ToListAsync());

    /// <summary>Envia um formulário: pega o token antiforgery na página <paramref name="formPage"/> e faz o POST em <paramref name="action"/>.</summary>
    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, string formPage, string action, params (string Name, string Value)[] fields)
    {
        string page = await client.GetStringAsync(formPage);
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        List<KeyValuePair<string, string>> body = [.. fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value)), new("__RequestVerificationToken", token)];
        using FormUrlEncodedContent form = new(body);
        return await client.PostAsync(action, form);
    }

    public static Task<HttpResponseMessage> PostNewAsync(HttpClient client, params (string Name, string Value)[] fields) =>
        PostAsync(client, "/painel/anuncios/novo", "/painel/anuncios/novo", fields);

    public static async Task<string> BodyAsync(HttpResponseMessage response) => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    public static int IdFrom(HttpResponseMessage redirect)
    {
        Assert.AreEqual(HttpStatusCode.Redirect, redirect.StatusCode);
        return int.Parse(Regex.Match(redirect.Destination(), @"/painel/anuncios/(\d+)/editar").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    public void Dispose() => Harness.Dispose();
}

/// <summary>As telas de criar e editar o rascunho (US-008) de ponta a ponta no site de teste: salvar, reabrir, permissões e o que a página mostra.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DraftTests
#pragma warning restore CA1515
{
    private const string Writer = PanelFixture.WriterEmail;

    [TestMethod]
    public async Task US008S01_SalvarRascunhoCompleto_MostraRascunhoSalvo_EAoReabrirTodosOsCamposVoltam()
    {
        using DraftSite site = await DraftSite.StartAsync();

        HttpResponseMessage saved = await DraftSite.PostNewAsync(site.Writer,
            ("Title", "Honda Civic 2018"), ("Description", "Único dono"), ("CategoryId", "33"), ("Price", "R$ 62.000,00"), ("Cep", "13015-100"),
            ("Fields[brandId]", "1"), ("Fields[modelId]", "11"), ("Fields[modelYear]", "2018"), ("Fields[km]", "45000"));

        int id = DraftSite.IdFrom(saved);
        HttpResponseMessage reopened = await site.Writer.GetAsync($"/painel/anuncios/{id}/editar");
        string page = await DraftSite.BodyAsync(reopened);
        Assert.AreEqual(HttpStatusCode.OK, reopened.StatusCode);
        StringAssert.Contains(page, "Rascunho salvo");
        StringAssert.Contains(page, "value=\"Honda Civic 2018\"");
        StringAssert.Contains(page, "Único dono");
        StringAssert.Contains(page, "value=\"62.000,00\"");
        StringAssert.Contains(page, "value=\"13015-100\"");
        StringAssert.Contains(page, "value=\"Campinas\"");
        StringAssert.Contains(page, "value=\"SP\"");
        StringAssert.Contains(page, "value=\"45000\"");
        Assert.IsTrue(Regex.IsMatch(page, @"<option value=""1"" selected[^>]*>Honda</option>"), "marca Honda escolhida");
        Assert.IsTrue(Regex.IsMatch(page, @"<option value=""11"" selected[^>]*>Civic</option>"), "modelo Civic escolhido");
        Assert.IsTrue(Regex.IsMatch(page, @"<option value=""2018"" selected[^>]*>2018</option>"), "ano 2018 escolhido");
        StringAssert.Contains(page, "Salvar rascunho");
        Ad ad = await site.LoadAsync(id);
        Assert.AreEqual(6_200_000L, ad.PriceCents);
        Assert.AreEqual(AdStatus.Draft, ad.Status);
    }

    [TestMethod]
    public async Task US008S07_SoComOTitulo_Salva_EOsDemaisCamposVoltamVazios()
    {
        using DraftSite site = await DraftSite.StartAsync();

        HttpResponseMessage saved = await DraftSite.PostNewAsync(site.Writer, ("Title", "Moto para retirar peças"));

        int id = DraftSite.IdFrom(saved);
        string page = await DraftSite.BodyAsync(await site.Writer.GetAsync($"/painel/anuncios/{id}/editar"));
        StringAssert.Contains(page, "value=\"Moto para retirar peças\"");
        Assert.IsFalse(Regex.IsMatch(page, @"<option value=""\d+"" selected"), "nenhuma categoria escolhida");
        Assert.IsTrue(Regex.IsMatch(page, @"id=""preco""[^>]*value=""""|name=""Price"" value=""""|name=""Price""(?![^>]*value=)"), "preço vazio");
    }

    [TestMethod]
    public async Task US008S08_SemTitulo_MostraAMensagemJuntoAoCampo_ENadaEhCriado()
    {
        using DraftSite site = await DraftSite.StartAsync();

        HttpResponseMessage response = await DraftSite.PostNewAsync(site.Writer, ("Title", ""), ("Description", "algo"), ("CategoryId", "33"));

        string page = await DraftSite.BodyAsync(response);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(page, "Informe um título");
        StringAssert.Contains(page, "id=\"error-Title\"");
        StringAssert.Contains(page, "aria-describedby=\"contador-titulo error-Title\"");
        StringAssert.Contains(page, "algo", "o que a pessoa digitou não se perde");
        Assert.AreEqual(0, await site.CountAsync());
    }

    [TestMethod]
    public async Task US008S09_Fragmento_MudaOsCamposDeAcordoComACategoria()
    {
        using DraftSite site = await DraftSite.StartAsync();

        string cars = await DraftSite.BodyAsync(await site.Writer.GetAsync("/painel/anuncios/campos?categoryId=33"));
        string land = await DraftSite.BodyAsync(await site.Writer.GetAsync("/painel/anuncios/campos?categoryId=30"));
        string books = await DraftSite.BodyAsync(await site.Writer.GetAsync("/painel/anuncios/campos?categoryId=86"));
        string jobs = await DraftSite.BodyAsync(await site.Writer.GetAsync("/painel/anuncios/campos?categoryId=96"));
        string services = await DraftSite.BodyAsync(await site.Writer.GetAsync("/painel/anuncios/campos?categoryId=66"));

        foreach (string name in new[] { "Fields[brandId]", "Fields[modelId]", "Fields[modelYear]", "Fields[versionId]", "Fields[km]" })
        {
            StringAssert.Contains(cars, $"name=\"{name}\"");
            Assert.IsFalse(land.Contains($"name=\"{name}\"", StringComparison.Ordinal), $"{name} some em terrenos");
        }

        StringAssert.Contains(land, "name=\"Fields[areaM2]\"");
        StringAssert.Contains(land, "name=\"Fields[propertyTypeId]\"");
        StringAssert.Contains(books, "name=\"Fields[conditionId]\"");
        StringAssert.Contains(books, "name=\"Fields[productType]\"");
        Assert.IsFalse(books.Contains("name=\"Fields[km]\"", StringComparison.Ordinal));
        StringAssert.Contains(cars, "maxlength=\"120\"");
        StringAssert.Contains(jobs, "maxlength=\"90\"");
        StringAssert.Contains(jobs, "0/90");
        StringAssert.Contains(jobs, "0/6000");
        StringAssert.Contains(jobs, "Vagas de emprego não têm fotos");
        StringAssert.Contains(jobs, "Informações adicionais");
        StringAssert.Contains(jobs, ">Salário");
        StringAssert.Contains(jobs, "name=\"Fields[jobAreaIds]\"");
        Assert.IsFalse(jobs.Contains("Fotos (", StringComparison.Ordinal), "sem contagem de fotos em vagas");
        Assert.IsFalse(services.Contains("name=\"Price\"", StringComparison.Ordinal), "serviços não têm preço");
        StringAssert.Contains(services, "Informações adicionais");
        StringAssert.Contains(services, "0/6000");
        StringAssert.Contains(services, "Fotos (0 de 6)");
        StringAssert.Contains(cars, "Fotos (0 de 20)");
    }

    [TestMethod]
    public async Task US008S09_SemJavaScript_AtualizarCampos_RefazOFormularioSemSalvar_EMantemOQueFoiDigitado()
    {
        using DraftSite site = await DraftSite.StartAsync();

        HttpResponseMessage response = await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", "/painel/anuncios/novo/atualizar",
            ("Title", "Terreno no centro"), ("Description", "Plano"), ("Price", "80.000,00"), ("CategoryId", "30"), ("Fields[km]", "5000"));

        string page = await DraftSite.BodyAsync(response);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(page, "name=\"Fields[areaM2]\"");
        Assert.IsFalse(page.Contains("name=\"Fields[km]\"", StringComparison.Ordinal), "o campo de outro grupo não volta");
        StringAssert.Contains(page, "value=\"Terreno no centro\"");
        StringAssert.Contains(page, "value=\"80.000,00\"");
        StringAssert.Contains(page, "id=\"atualizar-campos\"");
        StringAssert.Contains(page, "somente-sem-js");
        Assert.AreEqual(0, await site.CountAsync(), "atualizar os campos não salva nada");
    }

    [TestMethod]
    public async Task AtualizarCampos_AoTrocarDeCategoria_MantemSoAsOpcoesQueExistemNaNova()
    {
        using DraftSite site = await DraftSite.StartAsync();

        // 86 (Livros) e 85 (Decorações) usam o mesmo grupo: a condição escolhida continua; trocando para Carros, a condição some e o câmbio vem vazio
        string books = await DraftSite.BodyAsync(await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", "/painel/anuncios/novo/atualizar",
            ("Title", "X"), ("CategoryId", "86"), ("Fields[conditionId]", "2")));
        string cars = await DraftSite.BodyAsync(await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", "/painel/anuncios/novo/atualizar",
            ("Title", "X"), ("CategoryId", "33"), ("Fields[conditionId]", "2"), ("Fields[transmissionId]", "9999")));

        Assert.IsTrue(Regex.IsMatch(books, @"<option value=""2"" selected[^>]*>Usado - Excelente</option>"));
        Assert.IsFalse(cars.Contains("conditionId", StringComparison.Ordinal));
        Assert.IsFalse(Regex.IsMatch(cars, @"id=""campo-transmissionId""[\s\S]*?<option value=""9999"""), "opção inexistente não vira opção");
        Assert.IsFalse(Regex.IsMatch(cars, @"<option value=""\d+"" selected[^>]*>[^<]*</option>[\s\S]{0,400}id=""campo-doorsId"""), "nada de selecionado por engano");
    }

    [TestMethod]
    public async Task US008S10_RedatorAbreRascunhoDeOutro_VeAMensagem_SemOConteudo()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await site.AddAdAsync(PanelFixture.AdminEmail, title: "Segredo do outro");

        HttpResponseMessage get = await site.Writer.GetAsync($"/painel/anuncios/{id}/editar");
        HttpResponseMessage post = await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", $"/painel/anuncios/{id}/editar", ("Title", "Invadido"));

        Assert.AreEqual(HttpStatusCode.Forbidden, get.StatusCode);
        string page = await DraftSite.BodyAsync(get);
        StringAssert.Contains(page, "Você não tem permissão para acessar este anúncio");
        Assert.IsFalse(page.Contains("Segredo do outro", StringComparison.Ordinal));
        Assert.AreEqual(HttpStatusCode.Forbidden, post.StatusCode);
        Assert.AreEqual("Segredo do outro", (await site.LoadAsync(id)).Title);
    }

    [TestMethod]
    public async Task AnuncioInexistente_Da404()
    {
        using DraftSite site = await DraftSite.StartAsync();

        Assert.AreEqual(HttpStatusCode.NotFound, (await site.Writer.GetAsync("/painel/anuncios/99999/editar")).StatusCode);
    }

    [TestMethod]
    public async Task US008S11_Rejeitado_MostraOMotivoNoTopo_PermiteSalvar_ESituacaoContinuaRejeitado()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await site.AddAdAsync(Writer, AdStatus.Rejected, categoryId: 33);

        string page = await DraftSite.BodyAsync(await site.Writer.GetAsync($"/painel/anuncios/{id}/editar"));

        Assert.IsTrue(page.IndexOf("Fotos escuras", StringComparison.Ordinal) < page.IndexOf("<form asp", StringComparison.Ordinal) || page.IndexOf("Fotos escuras", StringComparison.Ordinal) < page.IndexOf("data-ad-form", StringComparison.Ordinal), "motivo antes do formulário");
        StringAssert.Contains(page, "data-rejection");
        StringAssert.Contains(page, "Situação: <strong>Rejeitado</strong>");

        HttpResponseMessage saved = await DraftSite.PostAsync(site.Writer, $"/painel/anuncios/{id}/editar", $"/painel/anuncios/{id}/editar",
            ("Title", "Honda Civic 2018 revisado"), ("CategoryId", "33"), ("Price", "58.000,00"), ("RowVersion", ""));
        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        Ad ad = await site.LoadAsync(id);
        Assert.AreEqual(AdStatus.Rejected, ad.Status);
        Assert.AreEqual("Honda Civic 2018 revisado", ad.Title);
        Assert.AreEqual("Fotos escuras", ad.RejectionReason);
        StringAssert.Contains(await DraftSite.BodyAsync(await site.Writer.GetAsync($"/painel/anuncios/{id}/editar")), "Salvar</button>");
    }

    [TestMethod]
    public async Task US008S12_EmRevisao_RedatorVeSoLeitura_ComAMensagem_ESemCamposEditaveis()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await site.AddAdAsync(Writer, AdStatus.InReview, "Em análise", 33);

        HttpResponseMessage response = await site.Writer.GetAsync($"/painel/anuncios/{id}/editar");

        string page = await DraftSite.BodyAsync(response);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(page, "Este anúncio está em revisão e não pode ser editado");
        StringAssert.Contains(page, "Em análise");
        Assert.IsFalse(page.Contains("data-ad-form", StringComparison.Ordinal));
        Assert.IsFalse(page.Contains("name=\"Title\"", StringComparison.Ordinal));
        Assert.IsFalse(page.Contains("<textarea", StringComparison.Ordinal));
        HttpResponseMessage post = await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", $"/painel/anuncios/{id}/editar", ("Title", "Forçado"));
        Assert.AreEqual(HttpStatusCode.Forbidden, post.StatusCode);
        Assert.AreEqual("Em análise", (await site.LoadAsync(id)).Title);
    }

    [TestMethod]
    public async Task US008S13_AdministradorEditaPublicado_BotaoSalvar_ESituacaoContinuaPublicado()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await site.AddAdAsync(Writer, AdStatus.Published, categoryId: 33, price: 6_200_000);

        string page = await DraftSite.BodyAsync(await site.Admin.GetAsync($"/painel/anuncios/{id}/editar"));
        StringAssert.Contains(page, "Salvar</button>");
        Assert.IsFalse(page.Contains("Salvar rascunho", StringComparison.Ordinal));

        HttpResponseMessage saved = await DraftSite.PostAsync(site.Admin, $"/painel/anuncios/{id}/editar", $"/painel/anuncios/{id}/editar",
            ("Title", "Honda Civic 2018"), ("CategoryId", "33"), ("Price", "R$ 59.000,00"));

        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        Ad ad = await site.LoadAsync(id);
        Assert.AreEqual(AdStatus.Published, ad.Status);
        Assert.AreEqual(5_900_000L, ad.PriceCents);
        Assert.AreEqual(await site.UserIdAsync(Writer), ad.AuthorId);
    }

    [TestMethod]
    public async Task Xss_TextoComScript_EhMostradoComoTexto_NoFormularioENaLeitura()
    {
        using DraftSite site = await DraftSite.StartAsync();
        const string script = "<script>alert('x')</script>";
        int id = DraftSite.IdFrom(await DraftSite.PostNewAsync(site.Writer, ("Title", script), ("Description", script), ("CategoryId", "33")));
        int inReview = await site.AddAdAsync(Writer, AdStatus.InReview, script, 33);

        string form = await site.Writer.GetStringAsync($"/painel/anuncios/{id}/editar");
        string read = await site.Writer.GetStringAsync($"/painel/anuncios/{inReview}/editar");

        foreach (string page in new[] { form, read })
        {
            Assert.IsFalse(page.Contains("<script>alert", StringComparison.Ordinal), "o script não pode aparecer como marca");
            StringAssert.Contains(page, "&lt;script&gt;alert(");
        }
    }

    [TestMethod]
    public async Task PostComSituacaoEAutorNoCorpo_NaoAlteraNada()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int adminId = await site.UserIdAsync(PanelFixture.AdminEmail);
        int writerId = await site.UserIdAsync(Writer);

        int id = DraftSite.IdFrom(await DraftSite.PostNewAsync(site.Writer,
            ("Title", "Honda"), ("Status", "3"), ("AuthorId", adminId.ToString()), ("PublishedAt", "2026-01-01"), ("PublishedById", adminId.ToString()),
            ("RejectionReason", "x"), ("CreatedBy", adminId.ToString())));
        await DraftSite.PostAsync(site.Writer, $"/painel/anuncios/{id}/editar", $"/painel/anuncios/{id}/editar", ("Title", "Honda 2"), ("Status", "5"), ("AuthorId", adminId.ToString()));

        Ad ad = await site.LoadAsync(id);
        Assert.AreEqual(AdStatus.Draft, ad.Status);
        Assert.AreEqual(writerId, ad.AuthorId);
        Assert.IsNull(ad.PublishedAt);
        Assert.IsNull(ad.PublishedById);
        Assert.IsNull(ad.RejectionReason);
        Assert.AreEqual("Honda 2", ad.Title);
    }

    [TestMethod]
    public async Task VariasOpcoes_ChegamComoLista_ESaoGravadasNaOrdemSemRepetir()
    {
        using DraftSite site = await DraftSite.StartAsync();

        int id = DraftSite.IdFrom(await DraftSite.PostNewAsync(site.Writer,
            ("Title", "Pizzaiolo"), ("CategoryId", "96"), ("Fields[jobAreaIds]", "1"), ("Fields[jobAreaIds]", "3"), ("Fields[jobAreaIds]", "3")));

        Assert.IsTrue(AdAttributes.TryParse((await site.LoadAsync(id)).Attributes, out AdAttributes attributes));
        CollectionAssert.AreEqual(new[] { 1, 3 }, attributes.GetInts("jobAreaIds").ToArray());
        string page = await site.Writer.GetStringAsync($"/painel/anuncios/{id}/editar");
        Assert.IsTrue(Regex.IsMatch(page, @"id=""campo-jobAreaIds-1""[^>]*checked"));
        Assert.IsTrue(Regex.IsMatch(page, @"id=""campo-jobAreaIds-3""[^>]*checked"));
        Assert.IsFalse(Regex.IsMatch(page, @"id=""campo-jobAreaIds-2""[^>]*checked"));
    }

    [TestMethod]
    public async Task TipoErradoNoCorpo_DaRecusaDeFormulario_NuncaErro500()
    {
        using DraftSite site = await DraftSite.StartAsync();

        HttpResponseMessage badCategory = await DraftSite.PostNewAsync(site.Writer, ("Title", "Honda"), ("CategoryId", "abc"));
        HttpResponseMessage hugeCategory = await DraftSite.PostNewAsync(site.Writer, ("Title", "Honda"), ("CategoryId", "99999999999999999999"));
        HttpResponseMessage textInKm = await DraftSite.PostNewAsync(site.Writer, ("Title", "Honda"), ("CategoryId", "33"), ("Fields[km]", "{\"$ne\":1}"));
        HttpResponseMessage arrayInKm = await DraftSite.PostNewAsync(site.Writer, ("Title", "Honda"), ("CategoryId", "33"), ("Fields[km]", "1"), ("Fields[km]", "2"));
        HttpResponseMessage badManual = await DraftSite.PostNewAsync(site.Writer, ("Title", "Honda"), ("LocationManual", "talvez"));
        HttpResponseMessage badRowVersion = await DraftSite.PostAsync(site.Writer, "/painel/anuncios/novo", "/painel/anuncios/99999/editar", ("Title", "x"), ("RowVersion", "###"));

        foreach (HttpResponseMessage response in new[] { badCategory, hugeCategory, textInKm, arrayInKm })
        {
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "volta ao formulário com a recusa");
            StringAssert.Contains(await DraftSite.BodyAsync(response), "text-danger");
        }

        StringAssert.Contains(await DraftSite.BodyAsync(badCategory), "Escolha uma categoria da lista");
        Assert.AreEqual(HttpStatusCode.Redirect, badManual.StatusCode, "um booleano torto é tratado como falso, sem derrubar a página");
        Assert.AreEqual(HttpStatusCode.NotFound, badRowVersion.StatusCode);
        Assert.AreEqual(1, await site.CountAsync(), "só o rascunho do booleano torto foi criado");
    }

    [TestMethod]
    public async Task Cep_ResolvidoNoServidor_QuandoOJavaScriptNaoRodou()
    {
        using DraftSite site = await DraftSite.StartAsync();

        int id = DraftSite.IdFrom(await DraftSite.PostNewAsync(site.Writer, ("Title", "Casa"), ("Cep", "13015-100")));

        Ad ad = await site.LoadAsync(id);
        Assert.AreEqual("Campinas", ad.City);
        Assert.AreEqual("SP", ad.Uf);
        Assert.AreEqual(1, site.Harness.Lookup.Calls);
    }

    [TestMethod]
    public async Task Cep_Inexistente_SalvaSemCidade_EAvisaNaTelaSeguinte()
    {
        using DraftSite site = await DraftSite.StartAsync();
        site.Harness.Lookup.Respond = _ => null;

        HttpResponseMessage saved = await DraftSite.PostNewAsync(site.Writer, ("Title", "Casa"), ("Cep", "99999-999"));

        int id = DraftSite.IdFrom(saved);
        string page = await DraftSite.BodyAsync(await site.Writer.GetAsync($"/painel/anuncios/{id}/editar"));
        StringAssert.Contains(page, "CEP não encontrado. Confira os números.");
        Assert.IsNull((await site.LoadAsync(id)).City);
    }

    [TestMethod]
    public async Task US008S14_ServicoDeCepForaDoAr_SalvaEReabreNoModoManual_ComListaDeCidades()
    {
        using DraftSite site = await DraftSite.StartAsync();
        site.Harness.Lookup.Respond = _ => FakeCepLookup.Unavailable();

        HttpResponseMessage saved = await DraftSite.PostNewAsync(site.Writer, ("Title", "Casa"), ("Cep", "13015-100"));

        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        StringAssert.Contains(saved.Destination(), "manual=True");
        string page = await DraftSite.BodyAsync(await site.Writer.GetAsync(saved.Destination()));
        StringAssert.Contains(page, "Não foi possível buscar o CEP. Preencha Cidade e UF manualmente.");
        Assert.IsTrue(Regex.IsMatch(page, @"name=""LocationManual"" value=""true"""));
        Assert.IsTrue(Regex.IsMatch(page, @"<select[^>]*id=""uf-manual""[^>]*>"), "UF vira lista");
        Assert.IsFalse(Regex.IsMatch(page, @"<select[^>]*id=""uf-manual""[^>]*disabled"), "a lista manual está ativa");
        Assert.IsTrue(Regex.IsMatch(page, @"id=""cidade""[^>]*disabled"), "o campo automático fica desativado");

        // Escolhe UF e cidade e salva: o selo de conferência fica gravado
        int id = DraftSite.IdFrom(saved);
        await DraftSite.PostAsync(site.Writer, $"/painel/anuncios/{id}/editar", $"/painel/anuncios/{id}/editar",
            ("Title", "Casa"), ("Cep", "13015-100"), ("LocationManual", "true"), ("Uf", "SP"), ("City", "Campinas"));
        Ad ad = await site.LoadAsync(id);
        Assert.IsTrue(ad.LocationManual);
        Assert.AreEqual("Campinas", ad.City);
    }

    [TestMethod]
    public async Task CidadeForjadaNoCorpo_ERecusada_ComAMensagemJuntoAoCampo()
    {
        using DraftSite site = await DraftSite.StartAsync();

        HttpResponseMessage response = await DraftSite.PostNewAsync(site.Writer,
            ("Title", "Casa"), ("Cep", "13015-100"), ("City", "Atlantis"), ("Uf", "SP"), ("LocationCep", "13015100"));

        string page = await DraftSite.BodyAsync(response);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(page, "Escolha uma cidade da lista");
        Assert.AreEqual(0, await site.CountAsync());
    }

    [TestMethod]
    public async Task Anonimo_VaiParaOLogin_EOFragmentoTambemExigeLogin()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient anonymous = site.Harness.Anonymous();

        HttpResponseMessage page = await anonymous.GetAsync("/painel/anuncios/novo");
        HttpResponseMessage fragment = await anonymous.GetAsync("/painel/anuncios/campos?categoryId=33");

        Assert.AreEqual(HttpStatusCode.Redirect, page.StatusCode);
        StringAssert.Contains(page.Destination(), "/painel/entrar");
        Assert.AreEqual(HttpStatusCode.Redirect, fragment.StatusCode);
    }

    [TestMethod]
    public async Task PaginaNova_TemACategoriaEmGruposDe124Opcoes_EOBotaoSomenteSemJs()
    {
        using DraftSite site = await DraftSite.StartAsync();

        string page = await DraftSite.BodyAsync(await site.Writer.GetAsync("/painel/anuncios/novo"));

        string select = Regex.Match(page, @"<select[^>]*id=""categoria""[\s\S]*?</select>").Value;
        Assert.AreEqual(124, Regex.Matches(select, @"<option value=""\d+""").Count, "as 124 categorias postáveis");
        StringAssert.Contains(page, "<optgroup label=\"Automóveis, Peças e Acessórios\">");
        StringAssert.Contains(page, "Autopeças › ");
        StringAssert.Contains(page, "Salvar rascunho");
        StringAssert.Contains(page, "somente-sem-js");
        StringAssert.Contains(page, "data-fields-url=\"/painel/anuncios/campos\"");
    }
}

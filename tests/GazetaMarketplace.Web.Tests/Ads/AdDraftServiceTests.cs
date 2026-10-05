using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>
/// O serviço do rascunho (US-008) contra SQLite, com as categorias, o catálogo pequeno e as cidades reais. Cobre salvar com campos, os limites por
/// grupo, a conferência da cadeia do catálogo, o endereço (D4), a troca de categoria e a auditoria sem conteúdo (D6).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdDraftServiceTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Land = 30;
    private const int Services = 66;
    private const int Jobs = 96;
    private const int Books = 86;

    private static async Task<ValidationException> RefusedAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ValidationException error)
        {
            return error;
        }

        Assert.Fail("a gravação devia ter sido recusada");
        return null;
    }

    [TestMethod]
    public async Task US008S07_SoComOTitulo_SalvaORascunho_ComAutorSituacaoEAuditoria()
    {
        using DraftDb db = new();
        int author = await db.SignInAsync();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Moto para retirar peças"));

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.AreEqual("Moto para retirar peças", ad.Title);
        Assert.AreEqual(AdStatus.Draft, ad.Status);
        Assert.AreEqual(author, ad.AuthorId, "o autor vem do usuário logado, nunca do formulário");
        Assert.IsNull(ad.CategoryId);
        Assert.IsNull(ad.PriceCents);
        Assert.IsNull(ad.Description);
        Assert.IsNull(ad.Cep);
        Assert.AreEqual("{}", ad.Attributes);
        AuditEntry entry = (await db.AuditAsync()).Single();
        Assert.AreEqual("ad.create", entry.Action);
        Assert.AreEqual(result.Id.ToString(), entry.TargetId);
        Assert.AreEqual(LocationOutcome.None, result.Location);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("    ")]
    public async Task US008S08_SemTitulo_NadaEhCriado_ENaoHaAuditoria(string title)
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException error = await RefusedAsync(() => db.CreateAsync(DraftDb.Input(title, description: "texto")));

        Assert.AreEqual("Informe um título", error.Errors["Title"][0]);
        Assert.AreEqual(0, await db.CountAdsAsync());
        Assert.IsEmpty(await db.AuditAsync());
    }

    [TestMethod]
    public async Task SemUsuarioLogado_NaoCria()
    {
        using DraftDb db = new();

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(() => db.CreateAsync(DraftDb.Input()));
    }

    [TestMethod]
    public async Task US008S01_RascunhoCompleto_GravaTudoComOTipoCerto_ECidadeVemDoCep()
    {
        using DraftDb db = new();
        await db.SignInAsync();
        Dictionary<string, string[]> fields = DraftDb.Fields(("brandId", "1"), ("modelId", "11"), ("modelYear", "2018"), ("km", "45000"));

        AdDraftResult result = await db.CreateAsync(DraftDb.Input(
            "Honda Civic 2018", Cars, "Único dono", "R$ 62.000,00", "13015-100", fields: fields));

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.AreEqual(Cars, ad.CategoryId);
        Assert.AreEqual(6_200_000L, ad.PriceCents);
        Assert.AreEqual("13015100", ad.Cep, "só os dígitos");
        Assert.AreEqual("Campinas", ad.City);
        Assert.AreEqual("SP", ad.Uf);
        Assert.IsFalse(ad.LocationManual);
        Assert.AreEqual(LocationOutcome.Resolved, result.Location);
        Assert.IsTrue(AdAttributes.TryParse(ad.Attributes, out AdAttributes attributes));
        Assert.IsTrue(attributes.TryGetInt("km", out int km));
        Assert.AreEqual(45000, km, "km é número no JSON, não texto");
        Assert.IsTrue(attributes.TryGetInt("brandId", out int brand));
        Assert.AreEqual(1, brand);
        Assert.IsTrue(attributes.TryGetInt("modelYear", out int year));
        Assert.AreEqual(2018, year);
        CollectionAssert.AreEqual(new[] { "13015100" }, db.Cep.Asked);
    }

    [TestMethod]
    public async Task US008S09_AsDecisoesNuncaVemDoFormulario_PublicadoEmEAutorSaoDoServidor()
    {
        using DraftDb db = new();
        int author = await db.SignInAsync();
        db.Clock.Now = db.Clock.Now.AddMinutes(5);

        AdDraftResult result = await db.CreateAsync(DraftDb.Input());

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.AreEqual(author, ad.AuthorId);
        Assert.IsNull(ad.PublishedAt);
        Assert.IsNull(ad.PublishedById);
        Assert.IsNull(ad.RejectedAt);
        Assert.IsNull(ad.SentAt);
        Assert.AreEqual(db.Clock.GetUtcNow().UtcDateTime, ad.CreatedAt);
    }

    [TestMethod]
    [DataRow(Jobs, 90, true)]
    [DataRow(Jobs, 91, false)]
    [DataRow(Services, 120, true)]
    [DataRow(Services, 121, false)]
    [DataRow(Cars, 120, true)]
    [DataRow(Cars, 121, false)]
    [DataRow(Books, 120, true)]
    public async Task US008S09_LimiteDoTitulo_VemDoGrupoDaCategoria(int category, int length, bool accepted)
    {
        using DraftDb db = new();
        await db.SignInAsync();

        Task<AdDraftResult> save = db.CreateAsync(DraftDb.Input(new string('a', length), category));

        if (accepted)
        {
            Assert.IsGreaterThan(0, (await save).Id);
        }
        else
        {
            ValidationException error = await RefusedAsync(() => save);
            StringAssert.Contains(error.Errors["Title"][0], "no máximo");
        }
    }

    [TestMethod]
    [DataRow(Jobs, 6000, true)]
    [DataRow(Jobs, 6001, false)]
    [DataRow(Services, 6000, true)]
    [DataRow(Services, 6001, false)]
    [DataRow(Cars, 5000, true)]
    [DataRow(Cars, 5001, false)]
    public async Task LimiteDaDescricao_VemDoGrupoDaCategoria(int category, int length, bool accepted)
    {
        using DraftDb db = new();
        await db.SignInAsync();

        Task<AdDraftResult> save = db.CreateAsync(DraftDb.Input("Título", category, new string('d', length)));

        if (accepted)
        {
            Assert.IsGreaterThan(0, (await save).Id);
        }
        else
        {
            ValidationException error = await RefusedAsync(() => save);
            StringAssert.Contains(error.Errors["Description"][0], "caracteres");
        }
    }

    [TestMethod]
    public async Task QuebraDeLinha_ContaUmCaractere_ComoNoContadorDaTela()
    {
        using DraftDb db = new();
        await db.SignInAsync();
        // O navegador envia CRLF: 2.500 quebras são 5.000 caracteres no envio, mas 2.500 na tela
        string text = string.Concat(Enumerable.Repeat("\r\n", 2500));

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Título", Cars, text + new string('x', 2500)));

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.AreEqual(5000, ad.Description.Length);
        Assert.IsFalse(ad.Description.Contains('\r', StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Servicos_NaoTemPreco_OCampoEhIgnorado_ENaoGrava()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Diarista", Services, price: "150,00"));

        Assert.IsNull((await db.LoadAdAsync(result.Id)).PriceCents);
    }

    [TestMethod]
    public async Task Vagas_PrecoEhOSalario_EAceitaOMesmoFormato()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Pizzaiolo", Jobs, price: "R$ 2.800,00"));

        Assert.AreEqual(280_000L, (await db.LoadAdAsync(result.Id)).PriceCents);
    }

    [TestMethod]
    [DataRow("0", "entre R$ 0,01")]
    [DataRow("0,00", "entre R$ 0,01")]
    [DataRow("100.000.000,00", "entre R$ 0,01")]
    [DataRow("abc", "Informe o preço em reais")]
    [DataRow("12.5", "Informe o preço em reais")]
    public async Task PrecoZeroAcimaDoTetoOuTexto_ERecusado(string price, string message)
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException error = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Honda", Cars, price: price)));

        StringAssert.Contains(error.Errors["Price"][0], message);
        Assert.AreEqual(0, await db.CountAdsAsync());
    }

    [TestMethod]
    public async Task PrecoSemVirgula_SaoReais_NaoCentavos()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Honda", Cars, price: "62000"));

        Assert.AreEqual(6_200_000L, (await db.LoadAdAsync(result.Id)).PriceCents);
    }

    [TestMethod]
    public async Task TextoEmCampoNumerico_ERecusadoComAMensagemDoCampo_EOAnuncioNaoEhCriado()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException error = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Honda", Cars, fields: DraftDb.Fields(("km", "muito")))));

        StringAssert.Contains(error.Errors["Fields[km]"][0], "apenas números");
        Assert.AreEqual(0, await db.CountAdsAsync());
    }

    [TestMethod]
    public async Task OpcaoForaDaLista_ERecusada_E_TodosOsErrosVemDeUmaVez()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException error = await RefusedAsync(() => db.CreateAsync(DraftDb.Input(
            "", Cars, price: "abc", fields: DraftDb.Fields(("transmissionId", "999"), ("km", "x")))));

        CollectionAssert.AreEquivalent(new[] { "Title", "Price", "Fields[transmissionId]", "Fields[km]" }, error.Errors.Keys.ToArray());
    }

    [TestMethod]
    public async Task CategoriaQueNaoPostaOuNaoExiste_ERecusada()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        foreach (int bad in new[] { 2, 3, 99999, 24 })
        {
            ValidationException error = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Honda", bad)));
            Assert.IsTrue(error.Errors.ContainsKey("CategoryId"), $"categoria {bad}");
        }
    }

    [TestMethod]
    public async Task CadeiaDoCatalogo_ModeloDeOutraMarca_AnoQueNaoExiste_VersaoDeOutroAno_SaoRecusados()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException model = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Honda", Cars, fields: DraftDb.Fields(("brandId", "2"), ("modelId", "11")))));
        ValidationException year = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Honda", Cars, fields: DraftDb.Fields(("brandId", "1"), ("modelId", "11"), ("modelYear", "2022")))));
        ValidationException version = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Honda", Cars, fields: DraftDb.Fields(("brandId", "1"), ("modelId", "11"), ("modelYear", "2020"), ("versionId", "100")))));
        ValidationException unknownBrand = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Honda", Cars, fields: DraftDb.Fields(("brandId", "777")))));

        StringAssert.Contains(model.Errors["Fields[modelId]"][0], "não pertence à marca");
        StringAssert.Contains(year.Errors["Fields[modelYear]"][0], "não existe");
        StringAssert.Contains(version.Errors["Fields[versionId]"][0], "não pertence");
        Assert.IsTrue(unknownBrand.Errors.ContainsKey("Fields[brandId]"));
        Assert.AreEqual(0, await db.CountAdsAsync());
    }

    [TestMethod]
    public async Task CadeiaDoCatalogo_FilhoSemOPai_ERecusado_MasCadeiaIncompletaDeCimaParaBaixoPode()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException orphan = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Honda", Cars, fields: DraftDb.Fields(("modelId", "11")))));
        AdDraftResult partial = await db.CreateAsync(DraftDb.Input("Honda", Cars, fields: DraftDb.Fields(("brandId", "1"), ("modelId", "11"))));
        AdDraftResult full = await db.CreateAsync(DraftDb.Input("Honda", Cars, fields: DraftDb.Fields(("brandId", "1"), ("modelId", "11"), ("modelYear", "2019"), ("versionId", "100"))));

        StringAssert.Contains(orphan.Errors["Fields[modelId]"][0], "marca");
        Assert.IsGreaterThan(0, partial.Id);
        Assert.IsGreaterThan(0, full.Id);
    }

    [TestMethod]
    public async Task CadeiaDaMoto_ConsultaOCatalogoDeMotos_NaoODeCarros()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        // A marca 2 de motos é a BMW; o modelo 11 só existe em carros
        ValidationException error = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("CG", 36, fields: DraftDb.Fields(("brandId", "1"), ("modelId", "11")))));
        AdDraftResult ok = await db.CreateAsync(DraftDb.Input("CG", 36, fields: DraftDb.Fields(("brandId", "1"), ("modelId", "10"))));

        Assert.IsTrue(error.Errors.ContainsKey("Fields[modelId]"));
        Assert.IsGreaterThan(0, ok.Id);
    }

    [TestMethod]
    public async Task Cep_DoNavegadorComCidadeEUf_ESoConferido_NaoConsultaOServico()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Casa", 26, cep: "13015-100", city: "campinas", uf: "sp", locationCep: "13015100"));

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.AreEqual("Campinas", ad.City, "o nome oficial da lista, não o digitado");
        Assert.AreEqual("SP", ad.Uf);
        Assert.AreEqual(LocationOutcome.Informed, result.Location);
        Assert.IsEmpty(db.Cep.Asked);
    }

    [TestMethod]
    public async Task Cep_SemCidadeDoNavegador_OServidorConsultaOCep()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Casa", 26, cep: "13015100"));

        Assert.AreEqual("Campinas", (await db.LoadAdAsync(result.Id)).City);
        Assert.AreEqual(LocationOutcome.Resolved, result.Location);
        Assert.HasCount(1, db.Cep.Asked);
    }

    [TestMethod]
    public async Task Cep_QueMudouDepoisDaCidade_DescartaACidadeAntigaEConsultaDeNovo()
    {
        using DraftDb db = new();
        await db.SignInAsync();
        db.Cep.Respond = cep => new GazetaMarketplace.Core.Location.CepResult(cep, "São Paulo", "SP", "viacep");

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Casa", 26, cep: "01310100", city: "Campinas", uf: "SP", locationCep: "13015100"));

        Assert.AreEqual("São Paulo", (await db.LoadAdAsync(result.Id)).City);
        Assert.HasCount(1, db.Cep.Asked);
    }

    [TestMethod]
    public async Task Cep_Inexistente_SalvaSemCidade_ComAvisoParaAPessoa()
    {
        using DraftDb db = new();
        await db.SignInAsync();
        db.Cep.Respond = _ => FakeCepService.NotFound();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Casa", 26, cep: "99999999"));

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.AreEqual("99999999", ad.Cep);
        Assert.IsNull(ad.City);
        Assert.IsNull(ad.Uf);
        Assert.AreEqual(LocationOutcome.CepNotFound, result.Location);
    }

    [TestMethod]
    public async Task Cep_ServicoForaDoAr_SalvaSemCidade_EPedeOModoManual()
    {
        using DraftDb db = new();
        await db.SignInAsync();
        db.Cep.Respond = _ => FakeCepService.Unavailable();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Casa", 26, cep: "13015100"));

        Assert.IsNull((await db.LoadAdAsync(result.Id)).City);
        Assert.AreEqual(LocationOutcome.CepUnavailable, result.Location);
    }

    [TestMethod]
    [DataRow("1301510")]
    [DataRow("abcdefgh")]
    [DataRow("130151000")]
    public async Task Cep_Incompleto_ERecusado(string cep)
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException error = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Casa", 26, cep: cep)));

        Assert.AreEqual("Informe um CEP com 8 dígitos", error.Errors["Cep"][0]);
    }

    [TestMethod]
    public async Task CidadeForjada_NaoEstaNaListaDaUf_ERecusada_EUfInventadaTambem()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException city = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Casa", 26, cep: "13015100", city: "Atlantis", uf: "SP", locationCep: "13015100")));
        ValidationException uf = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Casa", 26, cep: "13015100", city: "Campinas", uf: "XX", locationCep: "13015100")));

        Assert.AreEqual("Escolha uma cidade da lista", city.Errors["City"][0]);
        Assert.AreEqual("Escolha um estado da lista", uf.Errors["Uf"][0]);
        Assert.AreEqual(0, await db.CountAdsAsync());
    }

    [TestMethod]
    public async Task UfSemListaDeCidades_PadronizaONomeDigitado_MantendoOsAcentos()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Casa", 26, cep: "69900000", city: "  rio   BRANCO ", uf: "ac", locationCep: "69900000"));

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.AreEqual("Rio Branco", ad.City);
        Assert.AreEqual("AC", ad.Uf);
    }

    [TestMethod]
    public async Task ModoManual_GravaOSeloDeConferencia_ESemCidadeFicaPendente()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        AdDraftResult chosen = await db.CreateAsync(DraftDb.Input("Casa", 26, cep: "13015100", city: "Campinas", uf: "SP", manual: true));
        AdDraftResult pending = await db.CreateAsync(DraftDb.Input("Casa", 26, cep: "13015100", manual: true));

        Ad ad = await db.LoadAdAsync(chosen.Id);
        Assert.IsTrue(ad.LocationManual);
        Assert.AreEqual("Campinas", ad.City);
        Ad empty = await db.LoadAdAsync(pending.Id);
        Assert.IsNull(empty.City);
        Assert.IsFalse(empty.LocationManual, "sem cidade escolhida não há o que conferir");
        Assert.IsEmpty(db.Cep.Asked, "no modo manual o serviço de CEP não é consultado");
    }

    [TestMethod]
    public async Task CidadeSemCep_FforaDoModoManual_ERecusada()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        ValidationException error = await RefusedAsync(() => db.CreateAsync(DraftDb.Input("Casa", 26, city: "Campinas", uf: "SP")));

        Assert.IsTrue(error.Errors.ContainsKey("Cep"));
    }

    [TestMethod]
    public async Task US008S11_Atualizar_MantemASituacao_RejeitadoContinuaRejeitado()
    {
        using DraftDb db = new();
        int author = await db.SignInAsync();
        int id = await SeedAsync(db, author, AdStatus.Rejected);
        Ad before = await db.LoadAdAsync(id);

        AdDraftResult result = await db.UpdateAsync(id, before.RowVersion, DraftDb.Input("Título corrigido", Cars, price: "60.000,00"));

        Ad ad = await db.LoadAdAsync(id);
        Assert.AreEqual(AdStatus.Rejected, ad.Status);
        Assert.AreEqual("Fotos escuras", ad.RejectionReason, "o motivo continua visível");
        Assert.AreEqual("Título corrigido", ad.Title);
        Assert.AreEqual(6_000_000L, ad.PriceCents);
        Assert.AreEqual(id, result.Id);
    }

    [TestMethod]
    public async Task US008S13_AdministradorEditaPublicado_ASituacaoContinuaPublicado_EAutorNaoMuda()
    {
        using DraftDb db = new();
        int author = await db.SignInAsync();
        int id = await SeedAsync(db, author, AdStatus.Published, price: 6_200_000);
        await db.SignInAsync(administrator: true);
        Ad before = await db.LoadAdAsync(id);

        await db.UpdateAsync(id, before.RowVersion, DraftDb.Input("Honda Civic 2018", Cars, price: "R$ 59.000,00"));

        Ad ad = await db.LoadAdAsync(id);
        Assert.AreEqual(AdStatus.Published, ad.Status);
        Assert.AreEqual(5_900_000L, ad.PriceCents);
        Assert.AreEqual(author, ad.AuthorId);
        Assert.AreEqual(before.PublishedAt, ad.PublishedAt);
        Assert.AreEqual(before.PublishedById, ad.PublishedById);
    }

    [TestMethod]
    public async Task US008S12_EmRevisao_RedatorNaoSalva_EOAdministradorTambemNao_QuandoArquivado()
    {
        using DraftDb db = new();
        int author = await db.SignInAsync();
        int inReview = await SeedAsync(db, author, AdStatus.InReview);
        int archived = await SeedAsync(db, author, AdStatus.Archived);

        ForbiddenException review = await Assert.ThrowsExactlyAsync<ForbiddenException>(() => db.UpdateAsync(inReview, null, DraftDb.Input("Novo")));
        await db.SignInAsync(administrator: true);
        ForbiddenException old = await Assert.ThrowsExactlyAsync<ForbiddenException>(() => db.UpdateAsync(archived, null, DraftDb.Input("Novo")));

        Assert.AreEqual(AdMessages.InReviewReadOnly, review.Message);
        Assert.AreEqual(AdMessages.NotEditable, old.Message);
        Assert.AreEqual("Honda Civic 2018", (await db.LoadAdAsync(inReview)).Title);
    }

    [TestMethod]
    public async Task US008S10_RedatorNaoSalvaAnuncioDeOutro_ENadaAlteraNemAudita()
    {
        using DraftDb db = new();
        int owner = await db.SignInAsync();
        int id = await SeedAsync(db, owner, AdStatus.Draft);
        await db.SignInAsync();

        await Assert.ThrowsExactlyAsync<ForbiddenException>(() => db.UpdateAsync(id, null, DraftDb.Input("Invadido")));

        Assert.AreEqual("Honda Civic 2018", (await db.LoadAdAsync(id)).Title);
        Assert.IsEmpty(await db.AuditAsync());
    }

    [TestMethod]
    public async Task AnuncioInexistente_ENaoEncontrado()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        await Assert.ThrowsExactlyAsync<NotFoundException>(() => db.UpdateAsync(12345, null, DraftDb.Input()));
    }

    [TestMethod]
    public async Task US008S09_TrocarDeCategoria_MantemOsCamposComunsEDescartaOsDoGrupoAnterior()
    {
        using DraftDb db = new();
        int author = await db.SignInAsync();
        AdDraftResult created = await db.CreateAsync(DraftDb.Input(
            "Honda Civic 2018", Cars, "Texto", "62.000,00", fields: DraftDb.Fields(("brandId", "1"), ("km", "45000"), ("transmissionId", "1"))));
        Ad before = await db.LoadAdAsync(created.Id);

        // O formulário refeito para Terrenos só traz os campos de terrenos; os de veículo não são enviados
        await db.UpdateAsync(created.Id, before.RowVersion, DraftDb.Input("Honda Civic 2018", Land, "Texto", "62.000,00", fields: DraftDb.Fields(("areaM2", "450,75"))));

        Ad ad = await db.LoadAdAsync(created.Id);
        Assert.AreEqual(Land, ad.CategoryId);
        Assert.AreEqual("Texto", ad.Description);
        Assert.AreEqual(6_200_000L, ad.PriceCents);
        Assert.IsTrue(AdAttributes.TryParse(ad.Attributes, out AdAttributes attributes));
        CollectionAssert.AreEqual(new[] { "areaM2" }, attributes.Keys.ToArray(), "km, marca e câmbio ficaram para trás");
        Assert.IsTrue(attributes.TryGetDecimal("areaM2", out decimal area));
        Assert.AreEqual(450.75m, area);
        Assert.AreEqual(author, ad.AuthorId);
    }

    [TestMethod]
    public async Task US008S09_ValoresDeOutroGrupoEnviadosAMao_SaoIgnorados()
    {
        using DraftDb db = new();
        await db.SignInAsync();

        AdDraftResult result = await db.CreateAsync(DraftDb.Input("Terreno", Land, fields: DraftDb.Fields(("areaM2", "100"), ("km", "99999"), ("campoInventado", "x"))));

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.IsTrue(AdAttributes.TryParse(ad.Attributes, out AdAttributes attributes));
        CollectionAssert.AreEqual(new[] { "areaM2" }, attributes.Keys.ToArray());
    }

    [TestMethod]
    public async Task TrocarDeCategoria_ComMaisFotosQueOLimiteDoGrupoNovo_ERecusado()
    {
        using DraftDb db = new();
        int author = await db.SignInAsync();
        int id = await SeedAsync(db, author, AdStatus.Draft, categoryId: Cars);
        await db.WithContextAsync(async context =>
        {
            for (int i = 1; i <= 7; i++)
            {
                context.AdPhotos.Add(new AdPhoto { AdId = id, SortOrder = i, StorageKey = $"foto-{i}", Width = 1600, Height = 1200, SizeBytes = 100_000, OriginalKey = $"orig-{i}" });
            }

            await context.SaveChangesAsync();
            return 0;
        });
        Ad before = await db.LoadAdAsync(id);

        ValidationException error = await RefusedAsync(() => db.UpdateAsync(id, before.RowVersion, DraftDb.Input("Honda Civic 2018", Services)));
        AdDraftResult sameGroupLimit = await db.UpdateAsync(id, before.RowVersion, DraftDb.Input("Honda Civic 2018", Cars));

        Assert.AreEqual("Esta categoria aceita no máximo 6 fotos; remova fotos antes de trocar", error.Errors["CategoryId"][0]);
        Assert.AreEqual(Cars, (await db.LoadAdAsync(id)).CategoryId, "a categoria não mudou");
        Assert.AreEqual(id, sameGroupLimit.Id);
    }

    [TestMethod]
    public async Task Auditoria_GravaSoOsNomesDosCamposAlterados_NuncaOsValores_ETituloEDescricaoNaoEntram()
    {
        using DraftDb db = new();
        await db.SignInAsync();
        AdDraftResult created = await db.CreateAsync(DraftDb.Input(
            "Segredo no título", Cars, "Segredo na descrição", "62.000,00", fields: DraftDb.Fields(("brandId", "1"), ("km", "45000"))));
        Ad before = await db.LoadAdAsync(created.Id);

        await db.UpdateAsync(created.Id, before.RowVersion, DraftDb.Input(
            "Outro segredo", Cars, "Outro segredo ainda", "65.000,00", fields: DraftDb.Fields(("brandId", "1"), ("km", "50000"))));

        List<AuditEntry> entries = await db.AuditAsync();
        AuditEntry create = entries[0];
        AuditEntry update = entries[1];
        Assert.AreEqual("ad.create", create.Action);
        Assert.AreEqual("categoria: 33; preço (centavos): 6200000; campos: brandId, km", create.NewValue);
        Assert.IsNull(create.PreviousValue);
        Assert.AreEqual("ad.update", update.Action);
        Assert.AreEqual("preço (centavos): 6200000", update.PreviousValue);
        Assert.AreEqual("preço (centavos): 6500000; campos: km", update.NewValue, "só km mudou; brandId continuou igual");
        foreach (AuditEntry entry in entries)
        {
            Assert.IsFalse((entry.NewValue + entry.PreviousValue).Contains("segredo", StringComparison.OrdinalIgnoreCase), "título e descrição não são auditados");
            Assert.IsFalse((entry.NewValue + entry.PreviousValue).Contains("45000", StringComparison.Ordinal), "valores de campos não são auditados");
        }
    }

    [TestMethod]
    public async Task SalvarSemMudarNada_NaoGravaNemAudita()
    {
        using DraftDb db = new();
        await db.SignInAsync();
        AdDraftResult created = await db.CreateAsync(DraftDb.Input("Honda", Cars, "Texto", "62.000,00", fields: DraftDb.Fields(("km", "45000"))));
        Ad before = await db.LoadAdAsync(created.Id);

        AdDraftResult again = await db.UpdateAsync(created.Id, before.RowVersion, DraftDb.Input("Honda", Cars, "Texto", "62.000,00", fields: DraftDb.Fields(("km", "45000"))));

        Assert.AreEqual(created.Id, again.Id);
        Assert.HasCount(1, await db.AuditAsync(), "só a criação");
        Assert.AreEqual(before.UpdatedAt, (await db.LoadAdAsync(created.Id)).UpdatedAt);
    }

    [TestMethod]
    public async Task ApagarOPreco_VoltaANulo_ENaoAZero()
    {
        using DraftDb db = new();
        int author = await db.SignInAsync();
        int id = await SeedAsync(db, author, AdStatus.Draft, price: 5_000);
        Ad before = await db.LoadAdAsync(id);

        await db.UpdateAsync(id, before.RowVersion, DraftDb.Input("Honda", price: ""));

        Assert.IsNull((await db.LoadAdAsync(id)).PriceCents);
    }

    [TestMethod]
    public async Task TextoComScriptEhGuardadoComoTexto_SemSerExecutadoNemAlterado()
    {
        using DraftDb db = new();
        await db.SignInAsync();
        const string script = "<script>alert('x')</script>";

        AdDraftResult result = await db.CreateAsync(DraftDb.Input(script, Cars, script));

        Ad ad = await db.LoadAdAsync(result.Id);
        Assert.AreEqual(script, ad.Title);
        Assert.AreEqual(script, ad.Description);
    }

    private static async Task<int> SeedAsync(DraftDb db, int authorId, byte status, int? categoryId = null, long? price = null)
    {
        int decider = status is AdStatus.Published or AdStatus.Rejected or AdStatus.Archived ? await db.SignInAsync() : 99;
        Ad ad = AdFactory.At(status, authorId, "Honda Civic 2018", categoryId, decider);
        if (price is not null)
        {
            ad.SetPrice(price);
        }

        await db.WithContextAsync(async context =>
        {
            context.Ads.Add(ad);
            await context.SaveChangesAsync();
            return 0;
        });
        db.User.UserId = authorId;
        db.User.IsAdministrator = false;
        return ad.Id;
    }
}

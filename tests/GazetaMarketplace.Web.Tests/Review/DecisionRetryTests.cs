using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Infrastructure.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Review;

/// <summary>
/// As voltas de repetição da decisão (US-010-S07 e US-011): quando o <c>RowVersion</c> muda no meio, a decisão confere tudo de novo uma vez. O serviço de anúncios é
/// roteirizado — cada leitura e cada gravação devolvem o que o roteiro manda, na ordem — então o "meio da corrida" acontece sempre no mesmo ponto, sem depender de
/// threads (a corrida de verdade já é provada no SQL Server, nos testes de integração).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DecisionRetryTests
#pragma warning restore CA1515
{
    private const int AdId = 7;
    private const int Me = 1;
    private const int Other = 99;

    private static Ad Jobs(byte status, int decider = Other)
    {
        Ad ad = AdFactory.At(status, 5, "Pizzaiolo", 96, decider);
        ad.SetText("Pizzaiolo", "Descrição da vaga");
        ad.SetPrice(280_000);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        return ad;
    }

    private static CategoryTreeSnapshot Tree() => CategoryTreeSnapshot.Build([new CategoryRow(96, null, "Vagas de emprego", "vagas-de-emprego", 1, true, FieldGroupKeys.Jobs, false)]);

    private static async Task<T> WithReviewAsync<T>(ScriptedAds ads, Func<AdReview, AdDb, Task<T>> work)
    {
        using AdDb db = new();
        db.User.UserId = Me;
        db.User.IsAdministrator = true;
        return await db.WithContextAsync(async context =>
        {
            ads.Context = context;
            return await work(new AdReview(context, ads, new FixedTree(), new PhoneSettings(), db.User), db);
        });
    }

    private static async Task<T> WithTakedownAsync<T>(ScriptedAds ads, Func<AdTakedown, Task<T>> work)
    {
        using AdDb db = new();
        db.User.UserId = Me;
        db.User.IsAdministrator = true;
        return await db.WithContextAsync(async context =>
        {
            ads.Context = context;
            return await work(new AdTakedown(context, ads, db.User));
        });
    }

    [TestMethod]
    public async Task Rejeitar_UmConflitoDeVersao_RepeteUmaVezESegue_ESoltaOQueOContextoGuardava()
    {
        ScriptedAds ads = new(reads: [Jobs(AdStatus.InReview), Jobs(AdStatus.InReview)], writes: [Outcome.Conflict, Outcome.Ok]);

        ReviewResult result = await WithReviewAsync(ads, (review, _) => review.RejectAsync(AdId, "Fotos escuras", CancellationToken.None));

        Assert.AreEqual(ReviewOutcome.Done, result.Outcome);
        Assert.AreEqual(2, ads.Writes, "a decisão foi tentada duas vezes");
        Assert.AreEqual(2, ads.Reads, "cada tentativa leu o anúncio de novo");
        Assert.AreEqual(0, ads.TrackedAfterConflict, "o contexto foi limpo antes de tentar de novo");
    }

    [TestMethod]
    public async Task Rejeitar_DoisConflitosSeguidos_ComOAnuncioAindaEmRevisao_RepassaOErro_SemTerceiraTentativa()
    {
        ScriptedAds ads = new(reads: [Jobs(AdStatus.InReview), Jobs(AdStatus.InReview), Jobs(AdStatus.InReview)], writes: [Outcome.Conflict, Outcome.Conflict]);

        _ = await WithReviewAsync(ads, async (review, _) =>
        {
            await Assert.ThrowsExactlyAsync<ConflictException>(() => review.RejectAsync(AdId, "Fotos escuras", CancellationToken.None));
            return 0;
        });

        Assert.AreEqual(2, ads.Writes, "no máximo duas tentativas");
        Assert.AreEqual(3, ads.Reads, "duas leituras das tentativas e a conferência final");
    }

    [TestMethod]
    public async Task Rejeitar_PerdeACorrida_OutroAdministradorJaPublicou_DizPorOutroAdministrador_SemRepetir()
    {
        ScriptedAds ads = new(reads: [Jobs(AdStatus.InReview), Jobs(AdStatus.Published, Other)], writes: [Outcome.Conflict]);

        ReviewResult result = await WithReviewAsync(ads, (review, _) => review.RejectAsync(AdId, "Fotos escuras", CancellationToken.None));

        Assert.AreEqual(ReviewOutcome.AlreadyDecided, result.Outcome);
        Assert.AreEqual(AdMessages.AlreadyDecided(AdStatus.Published, false), result.Message);
        StringAssert.Contains(result.Message, "por outro administrador");
        Assert.AreEqual(1, ads.Writes, "o anúncio já estava decidido: não há o que tentar de novo");
    }

    [TestMethod]
    public async Task Rejeitar_DoisConflitos_EAoFinalOutroAdministradorJaRejeitou_DizPorOutroAdministrador()
    {
        ScriptedAds ads = new(reads: [Jobs(AdStatus.InReview), Jobs(AdStatus.InReview), Jobs(AdStatus.Rejected, Other)], writes: [Outcome.Conflict, Outcome.Conflict]);

        ReviewResult result = await WithReviewAsync(ads, (review, _) => review.RejectAsync(AdId, "Fotos escuras", CancellationToken.None));

        Assert.AreEqual(ReviewOutcome.AlreadyDecided, result.Outcome);
        StringAssert.Contains(result.Message, "já foi rejeitado por outro administrador");
        Assert.AreEqual(2, ads.Writes);
    }

    [TestMethod]
    public async Task Publicar_AnuncioEditadoNoMeio_ARepeticaoConfereDeNovo_EDevolvePendenciasEmVezDePublicarACorrigida()
    {
        // A primeira leitura está completa; entre as tentativas a descrição foi apagada numa edição, e a segunda leitura vê isso
        Ad edited = Jobs(AdStatus.InReview);
        edited.SetText("Pizzaiolo", null);
        ScriptedAds ads = new(reads: [Jobs(AdStatus.InReview), edited], writes: [Outcome.Conflict]);

        ReviewResult result = await WithReviewAsync(ads, (review, _) => review.PublishAsync(AdId, CancellationToken.None));

        Assert.AreEqual(ReviewOutcome.HasPending, result.Outcome);
        CollectionAssert.AreEqual(new[] { AdMessages.DescriptionRequired(FieldGroupRegistry.Get(FieldGroupKeys.Jobs).DescriptionLabel) }, result.Pending.Select(p => p.Message).ToArray());
        Assert.AreEqual(1, ads.Writes, "o anúncio com pendência não é publicado");
    }

    [TestMethod]
    public async Task Publicar_UmConflito_RepeteUmaVezESegue()
    {
        ScriptedAds ads = new(reads: [Jobs(AdStatus.InReview), Jobs(AdStatus.InReview)], writes: [Outcome.Conflict, Outcome.Ok]);

        ReviewResult result = await WithReviewAsync(ads, (review, _) => review.PublishAsync(AdId, CancellationToken.None));

        Assert.AreEqual(ReviewOutcome.Done, result.Outcome);
        Assert.AreEqual(2, ads.Writes);
    }

    [TestMethod]
    public async Task Arquivar_UmConflitoDeVersao_RepeteUmaVezESegue_ESoltaOContexto()
    {
        ScriptedAds ads = new(reads: [AdFactory.At(AdStatus.Published, 5, deciderId: Other), AdFactory.At(AdStatus.Published, 5, deciderId: Other)], writes: [Outcome.Conflict, Outcome.Ok]);

        TakedownResult result = await WithTakedownAsync(ads, takedown => takedown.ArchiveAsync(AdId, CancellationToken.None));

        Assert.AreEqual(TakedownOutcome.Done, result.Outcome);
        Assert.AreEqual(2, ads.Writes);
        Assert.AreEqual(2, ads.Reads);
        Assert.AreEqual(0, ads.TrackedAfterConflict);
    }

    [TestMethod]
    public async Task Arquivar_DoisConflitosSeguidos_ComOAnuncioAindaNaMesmaSituacao_RepassaOErro()
    {
        Ad Published() => AdFactory.At(AdStatus.Published, 5, deciderId: Other);
        ScriptedAds ads = new(reads: [Published(), Published(), Published()], writes: [Outcome.Conflict, Outcome.Conflict]);

        _ = await WithTakedownAsync(ads, async takedown =>
        {
            await Assert.ThrowsExactlyAsync<ConflictException>(() => takedown.ArchiveAsync(AdId, CancellationToken.None));
            return 0;
        });

        Assert.AreEqual(2, ads.Writes, "no máximo duas tentativas");
        Assert.AreEqual(3, ads.Reads);
    }

    [TestMethod]
    public async Task Arquivar_PerdeACorrida_OutroAdministradorJaArquivou_DizQueJaFoiArquivado_SemRepetir()
    {
        ScriptedAds ads = new(reads: [AdFactory.At(AdStatus.Published, 5, deciderId: Other), AdFactory.At(AdStatus.Archived, 5, deciderId: Other)], writes: [Outcome.Conflict]);

        TakedownResult result = await WithTakedownAsync(ads, takedown => takedown.ArchiveAsync(AdId, CancellationToken.None));

        Assert.AreEqual(TakedownOutcome.AlreadyDecided, result.Outcome);
        Assert.AreEqual(AdMessages.AlreadyArchived, result.Message);
        Assert.AreEqual(1, ads.Writes);
    }

    [TestMethod]
    public async Task Despublicar_PerdeACorrida_OAnuncioVoltouARascunho_DizOndeEleEstaAgora()
    {
        ScriptedAds ads = new(reads: [AdFactory.At(AdStatus.Published, 5, deciderId: Other), AdFactory.At(AdStatus.Draft, 5)], writes: [Outcome.Conflict]);

        TakedownResult result = await WithTakedownAsync(ads, takedown => takedown.UnpublishAsync(AdId, CancellationToken.None));

        Assert.AreEqual(TakedownOutcome.AlreadyDecided, result.Outcome);
        Assert.AreEqual(AdMessages.NoLongerPublished(AdStatus.Draft), result.Message);
        Assert.AreEqual(1, ads.Writes);
    }

    [TestMethod]
    public async Task Despublicar_DoisConflitos_EAoFinalOAnuncioJaSaiuDePublicado_DizASituacaoEmVezDeErro()
    {
        ScriptedAds ads = new(
            reads: [AdFactory.At(AdStatus.Published, 5, deciderId: Other), AdFactory.At(AdStatus.Published, 5, deciderId: Other), AdFactory.At(AdStatus.Archived, 5, deciderId: Other)],
            writes: [Outcome.Conflict, Outcome.Conflict]);

        TakedownResult result = await WithTakedownAsync(ads, takedown => takedown.UnpublishAsync(AdId, CancellationToken.None));

        Assert.AreEqual(TakedownOutcome.AlreadyDecided, result.Outcome);
        Assert.AreEqual(AdMessages.NoLongerPublished(AdStatus.Archived), result.Message);
    }

    private enum Outcome
    {
        Ok,
        Conflict
    }

    /// <summary>O <see cref="IAdService"/> do roteiro: lê e grava na ordem combinada e conta quantas vezes foi chamado.</summary>
    private sealed class ScriptedAds(IEnumerable<Ad> reads, IEnumerable<Outcome> writes) : IAdService
    {
        private readonly Queue<Ad> _reads = new(reads);
        private readonly Queue<Outcome> _writes = new(writes);

        public GazetaMarketplace.Infrastructure.Data.AppDbContext Context { get; set; }

        public int Reads { get; private set; }

        public int Writes { get; private set; }

        /// <summary>Quantas entidades o contexto ainda guardava na leitura seguinte a um conflito (precisa ser zero).</summary>
        public int TrackedAfterConflict { get; private set; }

        public Task<Ad> GetAsync(int id, CancellationToken cancellationToken)
        {
            Reads++;
            TrackedAfterConflict = Context.ChangeTracker.Entries().Count();
            return Task.FromResult(_reads.Dequeue());
        }

        public Task<Ad> GetForEditAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Ad> TransitionAsync(int id, byte target, string reason, CancellationToken cancellationToken)
        {
            Writes++;
            if (_writes.Dequeue() == Outcome.Conflict)
            {
                // O que a gravação que falhou deixou no contexto: a repetição precisa começar sem isso
                Context.Add(AdFactory.At(AdStatus.Draft));
                throw new ConflictException("Este anúncio foi alterado por outra pessoa");
            }

            return Task.FromResult<Ad>(null);
        }
    }

    private sealed class FixedTree : ICategoryTree
    {
        public Task<CategoryTreeSnapshot> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Tree());

        public void Invalidate()
        {
        }
    }

    private sealed class PhoneSettings : ISiteSettings
    {
        public Task<string> GetAsync(string key, CancellationToken cancellationToken) => Task.FromResult("19999999999");

        public Task<string> GetPhoneAsync(CancellationToken cancellationToken) => Task.FromResult("19999999999");

        public Task<bool> IsPhoneConfiguredAsync(CancellationToken cancellationToken) => Task.FromResult(true);

        public void Invalidate()
        {
        }
    }
}

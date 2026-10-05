using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>O <see cref="IAdService"/> sobre SQLite: autoria no servidor, passagens do Apêndice A, trilha de decisão e auditoria.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdServiceTests
#pragma warning restore CA1515
{
    private static Task<Ad> TransitionAsync(AdDb db, int adId, byte target, string reason = null) =>
        db.WithServiceAsync(service => service.TransitionAsync(adId, target, reason, CancellationToken.None));

    private static void ActAs(AdDb db, int? userId, bool admin)
    {
        db.User.UserId = userId;
        db.User.IsAdministrator = admin;
    }

    private static Task<List<AuditEntry>> AuditAsync(AdDb db) =>
        db.WithContextAsync(context => context.AuditEntries.AsNoTracking().Where(e => e.TargetType == "Ad").OrderBy(e => e.Id).ToListAsync());

    [TestMethod]
    public async Task Redator_NaoLeAnuncioDeOutro_NoServico() // US-008-S10
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int other = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author);
        ActAs(db, other, admin: false);

        ForbiddenException read = await Assert.ThrowsExactlyAsync<ForbiddenException>(() => db.WithServiceAsync(s => s.GetAsync(adId, CancellationToken.None)));
        ForbiddenException edit = await Assert.ThrowsExactlyAsync<ForbiddenException>(() => db.WithServiceAsync(s => s.GetForEditAsync(adId, CancellationToken.None)));

        Assert.AreEqual("Você não tem permissão para acessar este anúncio", read.Message);
        Assert.AreEqual("Você não tem permissão para acessar este anúncio", edit.Message);
    }

    [TestMethod]
    public async Task Autor_LeOProprioEmQualquerSituacao_EEditaSoRascunhoOuRejeitado()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        ActAs(db, author, admin: false);

        foreach (byte status in AdStatus.All)
        {
            int adId = await db.AddAdAsync(author, status);

            Ad read = await db.WithServiceAsync(s => s.GetAsync(adId, CancellationToken.None));
            Assert.AreEqual(status, read.Status);

            if (status is AdStatus.Draft or AdStatus.Rejected)
            {
                Ad edit = await db.WithServiceAsync(s => s.GetForEditAsync(adId, CancellationToken.None));
                Assert.AreEqual(adId, edit.Id);
            }
            else
            {
                ForbiddenException refused = await Assert.ThrowsExactlyAsync<ForbiddenException>(() => db.WithServiceAsync(s => s.GetForEditAsync(adId, CancellationToken.None)));
                Assert.AreEqual(status == AdStatus.InReview ? "Este anúncio está em revisão e não pode ser editado" : "Este anúncio não pode ser editado", refused.Message, AdStatus.Label(status));
            }
        }
    }

    [TestMethod]
    public async Task Administrador_LeQualquerUm_EEditaOsNaoArquivados()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int admin = await db.AddUserAsync();
        ActAs(db, admin, admin: true);

        foreach (byte status in AdStatus.All)
        {
            int adId = await db.AddAdAsync(author, status);
            Assert.AreEqual(adId, (await db.WithServiceAsync(s => s.GetAsync(adId, CancellationToken.None))).Id);

            if (status == AdStatus.Archived)
            {
                await Assert.ThrowsExactlyAsync<ForbiddenException>(() => db.WithServiceAsync(s => s.GetForEditAsync(adId, CancellationToken.None)));
            }
            else
            {
                Assert.AreEqual(adId, (await db.WithServiceAsync(s => s.GetForEditAsync(adId, CancellationToken.None))).Id);
            }
        }
    }

    [TestMethod]
    public async Task AnuncioInexistente_EhNaoEncontrado()
    {
        using AdDb db = new();
        ActAs(db, await db.AddUserAsync(), admin: true);

        NotFoundException error = await Assert.ThrowsExactlyAsync<NotFoundException>(() => TransitionAsync(db, 9999, AdStatus.InReview));

        Assert.AreEqual("NOT_FOUND", error.Code);
    }

    [TestMethod]
    public async Task AutorEnvia_Rascunho_ParaRevisao_ComTrilhaEAuditoria()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author);
        ActAs(db, author, admin: false);

        Ad sent = await TransitionAsync(db, adId, AdStatus.InReview);

        Ad stored = await db.LoadAdAsync(adId);
        Assert.AreEqual(AdStatus.InReview, sent.Status);
        Assert.AreEqual(AdStatus.InReview, stored.Status);
        Assert.AreEqual(db.Clock.Now.UtcDateTime, stored.SentAt);
        List<AuditEntry> audit = await AuditAsync(db);
        Assert.HasCount(1, audit);
        Assert.AreEqual("ad.submit", audit[0].Action);
        Assert.AreEqual(adId.ToString(System.Globalization.CultureInfo.InvariantCulture), audit[0].TargetId);
        Assert.AreEqual("Rascunho", audit[0].PreviousValue);
        Assert.AreEqual("Em revisão", audit[0].NewValue);
        Assert.AreEqual(author, audit[0].ActorId);
    }

    [TestMethod]
    public async Task Redator_NaoPublicaNemRejeitaNemArquivaNemDespublica_NemOProprio()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        ActAs(db, author, admin: false);

        (byte From, byte To)[] adminOnly = [.. AdStatusRules.All.Where(t => !t.AuthorAllowed).Select(t => (t.From, t.To))];
        foreach ((byte from, byte to) in adminOnly)
        {
            int adId = await db.AddAdAsync(author, from);

            await Assert.ThrowsExactlyAsync<ForbiddenException>(() => TransitionAsync(db, adId, to, "motivo"), $"{from} → {to}");

            Assert.AreEqual(from, (await db.LoadAdAsync(adId)).Status, "a recusa não muda a situação");
        }

        Assert.IsEmpty(await AuditAsync(db), "recusas não deixam rastro");
    }

    [TestMethod]
    public async Task OutroRedator_NaoEnviaOAnuncioDeAlguem()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int other = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author);
        ActAs(db, other, admin: false);

        await Assert.ThrowsExactlyAsync<ForbiddenException>(() => TransitionAsync(db, adId, AdStatus.InReview));

        Assert.AreEqual(AdStatus.Draft, (await db.LoadAdAsync(adId)).Status);
    }

    [TestMethod]
    public async Task PassagemInexistente_EhConflito_ArquivadoEDefinitivo()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int admin = await db.AddUserAsync();
        ActAs(db, admin, admin: true);

        int archived = await db.AddAdAsync(author, AdStatus.Archived);
        foreach (byte to in AdStatus.All)
        {
            ConflictException error = await Assert.ThrowsExactlyAsync<ConflictException>(() => TransitionAsync(db, archived, to, "motivo"));
            StringAssert.StartsWith(error.Message, "Não é possível mudar o anúncio de \"Arquivado\"");
        }

        int draft = await db.AddAdAsync(author);
        await Assert.ThrowsExactlyAsync<ConflictException>(() => TransitionAsync(db, draft, AdStatus.Published));
        Assert.IsEmpty(await AuditAsync(db));
    }

    [TestMethod]
    public async Task EnviarDuasVezes_SegundoEnvioEConflito_SoUmaEntradaNaAuditoria() // base do US-009-S05
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author);
        ActAs(db, author, admin: false);

        await TransitionAsync(db, adId, AdStatus.InReview);
        await Assert.ThrowsExactlyAsync<ConflictException>(() => TransitionAsync(db, adId, AdStatus.InReview));

        Assert.HasCount(1, await AuditAsync(db));
    }

    [TestMethod]
    public async Task AdministradorPublica_GravaQuemEQuando_ERejeitaComMotivo()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int admin = await db.AddUserAsync();
        int toPublish = await db.AddAdAsync(author, AdStatus.InReview);
        int toReject = await db.AddAdAsync(author, AdStatus.InReview);
        ActAs(db, admin, admin: true);

        await TransitionAsync(db, toPublish, AdStatus.Published);
        await TransitionAsync(db, toReject, AdStatus.Rejected, "Fotos escuras");

        Ad published = await db.LoadAdAsync(toPublish);
        Assert.AreEqual(admin, published.PublishedById);
        Assert.AreEqual(db.Clock.Now.UtcDateTime, published.PublishedAt);
        Ad rejected = await db.LoadAdAsync(toReject);
        Assert.AreEqual(admin, rejected.RejectedById);
        Assert.AreEqual("Fotos escuras", rejected.RejectionReason);
        CollectionAssert.AreEqual(new[] { "ad.publish", "ad.reject" }, (await AuditAsync(db)).Select(e => e.Action).ToArray());
    }

    [TestMethod]
    public async Task RejeitarSemMotivo_Recusa_SemRastro()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int admin = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author, AdStatus.InReview);
        ActAs(db, admin, admin: true);

        ValidationException error = await Assert.ThrowsExactlyAsync<ValidationException>(() => TransitionAsync(db, adId, AdStatus.Rejected, "  "));

        CollectionAssert.AreEqual(new[] { "Informe o motivo da rejeição" }, error.Errors["reason"]);
        Assert.AreEqual(AdStatus.InReview, (await db.LoadAdAsync(adId)).Status);
        Assert.IsEmpty(await AuditAsync(db));
    }

    [TestMethod]
    public async Task ReenviarRejeitado_LimpaARejeicao_EOHistoricoFicaNaAuditoria() // US-009-S04
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int admin = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author, AdStatus.InReview);
        ActAs(db, admin, admin: true);
        await TransitionAsync(db, adId, AdStatus.Rejected, "Fotos escuras");
        ActAs(db, author, admin: false);

        await TransitionAsync(db, adId, AdStatus.InReview);

        Ad stored = await db.LoadAdAsync(adId);
        Assert.AreEqual(AdStatus.InReview, stored.Status);
        Assert.IsNull(stored.RejectionReason);
        Assert.IsNull(stored.RejectedAt);
        Assert.IsNull(stored.RejectedById);
        CollectionAssert.AreEqual(new[] { "ad.reject", "ad.submit" }, (await AuditAsync(db)).Select(e => e.Action).ToArray());
    }

    [TestMethod]
    public async Task DespublicarArquivar_NaoDeixamRastroDePublicacaoNoRascunho()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int admin = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author, AdStatus.Published);
        ActAs(db, admin, admin: true);

        await TransitionAsync(db, adId, AdStatus.Draft);
        Ad unpublished = await db.LoadAdAsync(adId);
        Assert.AreEqual(AdStatus.Draft, unpublished.Status);
        Assert.IsNull(unpublished.PublishedAt);
        Assert.IsNull(unpublished.PublishedById);

        await TransitionAsync(db, adId, AdStatus.Archived);
        Ad archived = await db.LoadAdAsync(adId);
        Assert.AreEqual(db.Clock.Now.UtcDateTime, archived.ArchivedAt);
        Assert.AreEqual(admin, archived.ArchivedById, "quem arquivou fica gravado, como quem publicou e quem rejeitou");
        CollectionAssert.AreEqual(new[] { "ad.unpublish", "ad.archive" }, (await AuditAsync(db)).Select(e => e.Action).ToArray());
    }

    [TestMethod]
    public async Task SemIdentidade_NaoLeNemMuda()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author);
        ActAs(db, null, admin: false);

        await Assert.ThrowsExactlyAsync<ForbiddenException>(() => db.WithServiceAsync(s => s.GetAsync(adId, CancellationToken.None)));
        await Assert.ThrowsExactlyAsync<ForbiddenException>(() => TransitionAsync(db, adId, AdStatus.InReview));
    }

    [TestMethod]
    public async Task AnuncioSalvo_GuardaAuditoriaDeCriacaoEAtualizacaoPeloContexto()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        db.User.UserId = author;
        int adId = await db.AddAdAsync(author);
        db.Clock.Now = db.Clock.Now.AddHours(1);

        ActAs(db, author, admin: false);
        await TransitionAsync(db, adId, AdStatus.InReview);

        Ad stored = await db.LoadAdAsync(adId);
        Assert.AreEqual(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), stored.CreatedAt);
        Assert.AreEqual(author, stored.CreatedBy);
        Assert.AreEqual(new DateTime(2026, 10, 2, 13, 0, 0, DateTimeKind.Utc), stored.UpdatedAt);
        Assert.AreEqual(author, stored.UpdatedBy);
    }

    [TestMethod]
    public async Task FotoSalva_RecebeCreatedAtDoContexto()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        int adId = await db.AddAdAsync(author);
        db.Clock.Now = new DateTimeOffset(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);

        int photoId = await db.WithContextAsync(async context =>
        {
            AdPhoto photo = new() { AdId = adId, SortOrder = 0, StorageKey = "1_1600.webp", Width = 1600, Height = 1200, SizeBytes = 80_000, CreatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            context.AdPhotos.Add(photo);
            await context.SaveChangesAsync();
            return photo.Id;
        });

        AdPhoto stored = await db.WithContextAsync(context => context.AdPhotos.AsNoTracking().SingleAsync(p => p.Id == photoId));
        Assert.AreEqual(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc), stored.CreatedAt, "o carimbo é do contexto, não de quem chama");
        Assert.IsNull(stored.OriginalKey);
    }
}

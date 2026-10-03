using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Infrastructure.Ads;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// O <c>RowVersion</c> do SQL Server decidindo corridas sobre o mesmo anúncio (US-010: dois administradores; US-009-S05: clique duplo). O SQLite dos
/// testes de unidade não tem <c>rowversion</c> de verdade e serializa tudo, então só aqui a corrida é real.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdServiceConcurrencyTests
#pragma warning restore CA1515
{
    private static async Task<(bool Succeeded, Exception Error)> RunAsync(string connection, Barrier start, int userId, bool admin, int adId, byte target, string reason)
    {
        // Cada participante tem o seu contexto, o seu ator e a sua conexão, como duas requisições
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        FakeCurrentUser user = new() { UserId = userId, IsAdministrator = admin };
        AdService service = new(context, user, new AuditLog(context, user, TimeProvider.System), TimeProvider.System);

        // Os dois leem o anúncio antes de qualquer um gravar: é o que acontece com duas telas abertas
        _ = await context.Ads.AsNoTracking().SingleAsync(a => a.Id == adId);
        start.SignalAndWait();
        try
        {
            await service.TransitionAsync(adId, target, reason, CancellationToken.None);
            return (true, null);
        }
        catch (Exception error) when (error is ConflictException or ForbiddenException or ValidationException)
        {
            return (false, error);
        }
    }

    private static async Task<(bool Succeeded, Exception Error)[]> BothAsync(Func<Barrier, Task<(bool, Exception)>> first, Func<Barrier, Task<(bool, Exception)>> second)
    {
        using Barrier start = new(2);
        return await Task.WhenAll(Task.Run(() => first(start)), Task.Run(() => second(start)));
    }

    private static async Task<List<AuditEntry>> AuditAsync(string connection)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        return await context.AuditEntries.AsNoTracking().Where(e => e.TargetType == "Ad").OrderBy(e => e.Id).ToListAsync();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DoisAdministradoresDecidindoOMesmoAnuncio_SoUmDecide_AOutroRecebeConflito()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        int adminA = await AdData.AddUserAsync(connection);
        int adminB = await AdData.AddUserAsync(connection);

        for (int round = 1; round <= 6; round++)
        {
            int adId = await AdData.AddDraftAsync(connection, author, title: "Corrida " + round);
            await using (AppDbContext setup = SqlServerFixture.NewContext(connection))
            {
                Ad ad = await setup.Ads.SingleAsync(a => a.Id == adId);
                ad.ApplyTransition(AdStatus.InReview, author, DateTime.UtcNow, null);
                await setup.SaveChangesAsync();
            }

            (bool Succeeded, Exception Error)[] results = await BothAsync(
                start => RunAsync(connection, start, adminA, true, adId, AdStatus.Published, null),
                start => RunAsync(connection, start, adminB, true, adId, AdStatus.Rejected, "Fotos escuras"));

            Assert.AreEqual(1, results.Count(r => r.Succeeded), $"rodada {round}: exatamente um decide");
            ConflictException loser = results.Single(r => !r.Succeeded).Error as ConflictException;
            Assert.IsNotNull(loser, $"rodada {round}: quem perdeu recebe Conflito, não erro genérico");

            Ad stored = await AdData.LoadAsync(connection, adId);
            Assert.IsTrue(stored.Status is AdStatus.Published or AdStatus.Rejected);
            Assert.AreEqual(stored.Status == AdStatus.Published, stored.PublishedById is not null, "a trilha é só da decisão que valeu");
            Assert.AreEqual(stored.Status == AdStatus.Rejected, stored.RejectedById is not null);
        }

        List<AuditEntry> audit = await AuditAsync(connection);
        Assert.AreEqual(6, audit.Count, "uma entrada por decisão; a perdedora não deixa rastro (a auditoria vai na mesma transação)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CliqueDuploEmEnviar_GeraUmaUnicaPassagem() // US-009-S05
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);

        for (int round = 1; round <= 6; round++)
        {
            int adId = await AdData.AddDraftAsync(connection, author, title: "Clique " + round);

            (bool Succeeded, Exception Error)[] results = await BothAsync(
                start => RunAsync(connection, start, author, false, adId, AdStatus.InReview, null),
                start => RunAsync(connection, start, author, false, adId, AdStatus.InReview, null));

            Assert.AreEqual(1, results.Count(r => r.Succeeded), $"rodada {round}");
            Assert.IsInstanceOfType<ConflictException>(results.Single(r => !r.Succeeded).Error);
            Assert.AreEqual(AdStatus.InReview, (await AdData.LoadAsync(connection, adId)).Status);
        }

        Assert.AreEqual(6, (await AuditAsync(connection)).Count(e => e.Action == "ad.submit"), "um envio por anúncio");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task RowVersion_MudaACadaGravacao_EOContextoAntigoPerde()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        int adId = await AdData.AddDraftAsync(connection, author);
        byte[] first = (await AdData.LoadAsync(connection, adId)).RowVersion;

        await using AppDbContext stale = SqlServerFixture.NewContext(connection);
        Ad staleCopy = await stale.Ads.SingleAsync(a => a.Id == adId);

        await using (AppDbContext fresh = SqlServerFixture.NewContext(connection))
        {
            Ad ad = await fresh.Ads.SingleAsync(a => a.Id == adId);
            ad.SetPrice(5_000);
            await fresh.SaveChangesAsync();
        }

        CollectionAssert.AreNotEqual(first, (await AdData.LoadAsync(connection, adId)).RowVersion);
        staleCopy.SetPrice(9_000);
        await Assert.ThrowsExactlyAsync<ConflictException>(() => stale.SaveChangesAsync());
        Assert.AreEqual(5_000, (await AdData.LoadAsync(connection, adId)).PriceCents, "a edição velha não sobrescreve a nova");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task EdicaoDoRedatorEDecisaoDoAdministrador_ConcorrentesNaoSeSobrepoem()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        int admin = await AdData.AddUserAsync(connection);
        int adId = await AdData.AddDraftAsync(connection, author);
        await using (AppDbContext setup = SqlServerFixture.NewContext(connection))
        {
            (await setup.Ads.SingleAsync(a => a.Id == adId)).ApplyTransition(AdStatus.InReview, author, DateTime.UtcNow, null);
            await setup.SaveChangesAsync();
        }

        // O Redator abriu o formulário antes; o Administrador publica; o Redator tenta salvar com a versão velha
        await using AppDbContext writerContext = SqlServerFixture.NewContext(connection);
        Ad writerCopy = await writerContext.Ads.SingleAsync(a => a.Id == adId);
        (bool ok, _) = await RunAsync(connection, new Barrier(1), admin, true, adId, AdStatus.Published, null);
        Assert.IsTrue(ok);

        writerCopy.SetText("Título alterado depois da publicação", null);
        await Assert.ThrowsExactlyAsync<ConflictException>(() => writerContext.SaveChangesAsync());
        Assert.AreEqual(AdStatus.Published, (await AdData.LoadAsync(connection, adId)).Status);
        Assert.AreEqual("Honda Civic 2018", (await AdData.LoadAsync(connection, adId)).Title);
    }
}

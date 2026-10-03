using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AuditLogTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Registra_AtorAcaoAlvoEData()
    {
        using TestDatabase database = new();
        database.User.UserId = 7;
        database.User.CorrelationId = "corr-123";
        using (TestAppDbContext context = database.NewContext())
        {
            IAuditLog log = new AuditLog(context, database.User, database.Clock);
            await log.RecordAsync(new AuditRecord("anuncio.publicar", "Anuncio", "42",
                AuditResult.Success, "{\"situacao\":2}", "{\"situacao\":3}"), default);
        }

        using TestAppDbContext reading = database.NewContext();
        AuditEntry entry = await reading.AuditEntries.SingleAsync();
        Assert.AreEqual(7, entry.ActorId);
        Assert.AreEqual("anuncio.publicar", entry.Action);
        Assert.AreEqual("Anuncio", entry.TargetType);
        Assert.AreEqual("42", entry.TargetId);
        Assert.AreEqual(AuditResult.Success, entry.Result);
        Assert.AreEqual("{\"situacao\":2}", entry.PreviousValue);
        Assert.AreEqual("{\"situacao\":3}", entry.NewValue);
        Assert.AreEqual("corr-123", entry.CorrelationId);
        Assert.AreEqual(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), entry.OccurredAt);
        Assert.AreEqual(DateTimeKind.Utc, entry.OccurredAt.Kind);
    }

    [TestMethod]
    public async Task AcaoSemUsuario_RegistraAtorNulo_EResultadoNegado()
    {
        using TestDatabase database = new();
        using (TestAppDbContext context = database.NewContext())
        {
            await new AuditLog(context, database.User, database.Clock)
                .RecordAsync(new AuditRecord("usuario.desativar", "Usuario", "3", AuditResult.Denied), default);
        }

        using TestAppDbContext reading = database.NewContext();
        AuditEntry entry = await reading.AuditEntries.SingleAsync();
        Assert.IsNull(entry.ActorId);
        Assert.AreEqual(AuditResult.Denied, entry.Result);
    }

    [TestMethod]
    public async Task EntradaSemAcaoOuAlvo_E_Recusada()
    {
        using TestDatabase database = new();
        using TestAppDbContext context = database.NewContext();
        AuditLog log = new(context, database.User, database.Clock);

        await Assert.ThrowsAsync<ArgumentException>(() => log.RecordAsync(new AuditRecord(" ", "Anuncio", "1"), default));
        await Assert.ThrowsAsync<ArgumentException>(() => log.RecordAsync(new AuditRecord("a", "", "1"), default));
        await Assert.ThrowsAsync<ArgumentException>(() => log.RecordAsync(new AuditRecord("a", "Anuncio", null), default));
    }

    [TestMethod]
    public void NaoExisteOperacaoParaEditarOuApagarEntradas()
    {
        string[] methods = typeof(IAuditLog).GetMethods().Select(m => m.Name).ToArray();
        Assert.HasCount(1, methods);
        Assert.AreEqual("RecordAsync", methods[0]);

        // a entrada é imutável depois de criada: todas as propriedades são init-only
        foreach (PropertyInfo property in typeof(AuditEntry).GetProperties())
        {
            Type[] modifiers = property.SetMethod.ReturnParameter.GetRequiredCustomModifiers();
            Assert.IsTrue(modifiers.Contains(typeof(IsExternalInit)), property.Name + " deveria ser init-only");
        }
    }

    [TestMethod]
    public async Task SaveChanges_RecusaAlterarEntradaDeAuditoria()
    {
        using TestDatabase database = new();
        using TestAppDbContext context = database.NewContext();
        await new AuditLog(context, database.User, database.Clock)
            .RecordAsync(new AuditRecord("a.b", "Alvo", "1"), default);

        AuditEntry entry = await context.AuditEntries.SingleAsync();
        context.Entry(entry).Property(e => e.Action).CurrentValue = "adulterada";

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [TestMethod]
    public async Task SaveChanges_RecusaApagarEntradaDeAuditoria()
    {
        using TestDatabase database = new();
        using TestAppDbContext context = database.NewContext();
        await new AuditLog(context, database.User, database.Clock)
            .RecordAsync(new AuditRecord("a.b", "Alvo", "1"), default);

        context.AuditEntries.Remove(await context.AuditEntries.SingleAsync());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }
}

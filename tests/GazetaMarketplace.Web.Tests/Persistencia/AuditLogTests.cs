using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entidades;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistencia;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AuditLogTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Registra_AtorAcaoAlvoEData()
    {
        using BancoDeTestes banco = new();
        banco.Usuario.UsuarioId = 7;
        banco.Usuario.CorrelationId = "corr-123";
        using (AppDbContextDeTeste contexto = banco.NovoContexto())
        {
            IAuditLog log = new AuditLog(contexto, banco.Usuario, banco.Relogio);
            await log.RegistrarAsync(new EntradaDeAuditoria("anuncio.publicar", "Anuncio", "42",
                ResultadoDaAuditoria.Sucesso, "{\"situacao\":2}", "{\"situacao\":3}"), default);
        }

        using AppDbContextDeTeste leitura = banco.NovoContexto();
        AuditEntry entrada = await leitura.AuditEntries.SingleAsync();
        Assert.AreEqual(7, entrada.ActorId);
        Assert.AreEqual("anuncio.publicar", entrada.Action);
        Assert.AreEqual("Anuncio", entrada.TargetType);
        Assert.AreEqual("42", entrada.TargetId);
        Assert.AreEqual(ResultadoDaAuditoria.Sucesso, entrada.Result);
        Assert.AreEqual("{\"situacao\":2}", entrada.PreviousValue);
        Assert.AreEqual("{\"situacao\":3}", entrada.NewValue);
        Assert.AreEqual("corr-123", entrada.CorrelationId);
        Assert.AreEqual(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), entrada.OccurredAt);
        Assert.AreEqual(DateTimeKind.Utc, entrada.OccurredAt.Kind);
    }

    [TestMethod]
    public async Task AcaoSemUsuario_RegistraAtorNulo_EResultadoNegado()
    {
        using BancoDeTestes banco = new();
        using (AppDbContextDeTeste contexto = banco.NovoContexto())
        {
            await new AuditLog(contexto, banco.Usuario, banco.Relogio)
                .RegistrarAsync(new EntradaDeAuditoria("usuario.desativar", "Usuario", "3", ResultadoDaAuditoria.Negado), default);
        }

        using AppDbContextDeTeste leitura = banco.NovoContexto();
        AuditEntry entrada = await leitura.AuditEntries.SingleAsync();
        Assert.IsNull(entrada.ActorId);
        Assert.AreEqual(ResultadoDaAuditoria.Negado, entrada.Result);
    }

    [TestMethod]
    public async Task EntradaSemAcaoOuAlvo_E_Recusada()
    {
        using BancoDeTestes banco = new();
        using AppDbContextDeTeste contexto = banco.NovoContexto();
        AuditLog log = new(contexto, banco.Usuario, banco.Relogio);

        await Assert.ThrowsAsync<ArgumentException>(() => log.RegistrarAsync(new EntradaDeAuditoria(" ", "Anuncio", "1"), default));
        await Assert.ThrowsAsync<ArgumentException>(() => log.RegistrarAsync(new EntradaDeAuditoria("a", "", "1"), default));
        await Assert.ThrowsAsync<ArgumentException>(() => log.RegistrarAsync(new EntradaDeAuditoria("a", "Anuncio", null), default));
    }

    [TestMethod]
    public void NaoExisteOperacaoParaEditarOuApagarEntradas()
    {
        string[] metodos = typeof(IAuditLog).GetMethods().Select(m => m.Name).ToArray();
        Assert.HasCount(1, metodos);
        Assert.AreEqual("RegistrarAsync", metodos[0]);

        // a entrada é imutável depois de criada: todas as propriedades são init-only
        foreach (PropertyInfo propriedade in typeof(AuditEntry).GetProperties())
        {
            Type[] modificadores = propriedade.SetMethod.ReturnParameter.GetRequiredCustomModifiers();
            Assert.IsTrue(modificadores.Contains(typeof(IsExternalInit)), propriedade.Name + " deveria ser init-only");
        }
    }

    [TestMethod]
    public async Task SaveChanges_RecusaAlterarEntradaDeAuditoria()
    {
        using BancoDeTestes banco = new();
        using AppDbContextDeTeste contexto = banco.NovoContexto();
        await new AuditLog(contexto, banco.Usuario, banco.Relogio)
            .RegistrarAsync(new EntradaDeAuditoria("a.b", "Alvo", "1"), default);

        AuditEntry entrada = await contexto.AuditEntries.SingleAsync();
        contexto.Entry(entrada).Property(e => e.Action).CurrentValue = "adulterada";

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => contexto.SaveChangesAsync());
    }

    [TestMethod]
    public async Task SaveChanges_RecusaApagarEntradaDeAuditoria()
    {
        using BancoDeTestes banco = new();
        using AppDbContextDeTeste contexto = banco.NovoContexto();
        await new AuditLog(contexto, banco.Usuario, banco.Relogio)
            .RegistrarAsync(new EntradaDeAuditoria("a.b", "Alvo", "1"), default);

        contexto.AuditEntries.Remove(await contexto.AuditEntries.SingleAsync());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => contexto.SaveChangesAsync());
    }
}

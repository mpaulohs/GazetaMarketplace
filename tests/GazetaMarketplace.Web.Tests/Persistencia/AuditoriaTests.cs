using System;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistencia;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AuditoriaTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Salvar_PreencheCriacaoEAlteracao()
    {
        using BancoDeTestes banco = new();
        banco.Usuario.UsuarioId = 5;
        int id;
        using (AppDbContextDeTeste contexto = banco.NovoContexto())
        {
            EntidadeDeTeste entidade = new() { Nome = "primeira" };
            contexto.Entidades.Add(entidade);
            await contexto.SaveChangesAsync();
            id = entidade.Id;
        }

        banco.Relogio.Agora = new DateTimeOffset(2026, 10, 2, 13, 0, 0, TimeSpan.Zero);
        banco.Usuario.UsuarioId = 6;
        using (AppDbContextDeTeste contexto = banco.NovoContexto())
        {
            EntidadeDeTeste entidade = await contexto.Entidades.FindAsync(id);
            entidade.Nome = "editada";
            await contexto.SaveChangesAsync();
        }

        using AppDbContextDeTeste leitura = banco.NovoContexto();
        EntidadeDeTeste lida = await leitura.Entidades.FindAsync(id);
        Assert.AreEqual(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), lida.CreatedAt);
        Assert.AreEqual(5, lida.CreatedBy);
        Assert.AreEqual(new DateTime(2026, 10, 2, 13, 0, 0, DateTimeKind.Utc), lida.UpdatedAt);
        Assert.AreEqual(6, lida.UpdatedBy);
    }

    [TestMethod]
    public async Task AcaoDoSistema_GravaUsuarioNulo()
    {
        using BancoDeTestes banco = new();
        banco.Usuario.UsuarioId = null;
        using AppDbContextDeTeste contexto = banco.NovoContexto();
        EntidadeDeTeste entidade = new() { Nome = "do sistema" };
        contexto.Entidades.Add(entidade);

        await contexto.SaveChangesAsync();

        Assert.IsNull(entidade.CreatedBy);
        Assert.IsNull(entidade.UpdatedAt);
    }

    [TestMethod]
    public void SaveChangesSincrono_TambemPreencheAuditoria()
    {
        using BancoDeTestes banco = new();
        banco.Usuario.UsuarioId = 9;
        using AppDbContextDeTeste contexto = banco.NovoContexto();
        EntidadeDeTeste entidade = new() { Nome = "sync" };
        contexto.Entidades.Add(entidade);

        contexto.SaveChanges();

        Assert.AreEqual(9, entidade.CreatedBy);
        Assert.AreEqual(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), entidade.CreatedAt);
    }

    [TestMethod]
    public async Task EditarUmaLinha_NaoAlteraACriacao()
    {
        using BancoDeTestes banco = new();
        banco.Usuario.UsuarioId = 5;
        int id;
        using (AppDbContextDeTeste contexto = banco.NovoContexto())
        {
            EntidadeDeTeste entidade = new() { Nome = "a" };
            contexto.Entidades.Add(entidade);
            await contexto.SaveChangesAsync();
            id = entidade.Id;
        }

        using (AppDbContextDeTeste contexto = banco.NovoContexto())
        {
            EntidadeDeTeste entidade = await contexto.Entidades.FindAsync(id);
            entidade.Nome = "b";
            entidade.CreatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            entidade.CreatedBy = 99;
            await contexto.SaveChangesAsync();
        }

        using AppDbContextDeTeste leitura = banco.NovoContexto();
        EntidadeDeTeste lida = await leitura.Entidades.FindAsync(id);
        Assert.AreEqual(5, lida.CreatedBy);
        Assert.AreEqual(2026, lida.CreatedAt.Year);
    }

    [TestMethod]
    public async Task DatasLidasDoBanco_VoltamComoUtc()
    {
        using BancoDeTestes banco = new();
        using (AppDbContextDeTeste contexto = banco.NovoContexto())
        {
            contexto.Entidades.Add(new EntidadeDeTeste { Nome = "utc" });
            await contexto.SaveChangesAsync();
        }

        using AppDbContextDeTeste leitura = banco.NovoContexto();
        EntidadeDeTeste lida = await leitura.Entidades.FindAsync(1);
        Assert.AreEqual(DateTimeKind.Utc, lida.CreatedAt.Kind);
    }
}

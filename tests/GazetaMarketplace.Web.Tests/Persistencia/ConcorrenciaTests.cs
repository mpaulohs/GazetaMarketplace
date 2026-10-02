using System.Threading.Tasks;
using GazetaMarketplace.Core.Excecoes;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistencia;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ConcorrenciaTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task DuasEdicoes_DaMesmaLinha_GeramConflito()
    {
        using BancoDeTestes banco = new();
        using AppDbContextDeTeste primeiro = banco.NovoContexto();
        using AppDbContextDeTeste segundo = banco.NovoContexto();
        primeiro.Entidades.Add(new EntidadeDeTeste { Nome = "original" });
        await primeiro.SaveChangesAsync();

        EntidadeDeTeste doPrimeiro = await primeiro.Entidades.FindAsync(1);
        EntidadeDeTeste doSegundo = await segundo.Entidades.FindAsync(1);
        doPrimeiro.Nome = "da primeira pessoa";
        doSegundo.Nome = "da segunda pessoa";
        await primeiro.SaveChangesAsync();

        ConflictException erro = await Assert.ThrowsExactlyAsync<ConflictException>(() => segundo.SaveChangesAsync());
        Assert.AreEqual("CONFLICT", erro.Code);
        Assert.AreEqual(409, erro.StatusCode);
    }

    [TestMethod]
    public async Task EdicoesEmSequencia_NoMesmoContexto_NaoGeramConflito()
    {
        using BancoDeTestes banco = new();
        using AppDbContextDeTeste contexto = banco.NovoContexto();
        EntidadeDeTeste entidade = new() { Nome = "v1" };
        contexto.Entidades.Add(entidade);
        await contexto.SaveChangesAsync();

        entidade.Nome = "v2";
        await contexto.SaveChangesAsync();
        entidade.Nome = "v3";
        await contexto.SaveChangesAsync();

        Assert.AreEqual("v3", entidade.Nome);
    }
}

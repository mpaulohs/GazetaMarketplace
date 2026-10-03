using System;
using GazetaMarketplace.Web.Security;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>NFR-06 e US-006-S06: o bloqueio por origem conta falhas (não requisições) e usa o relógio injetado.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ContadorDeFalhasDeLoginTests
#pragma warning restore CA1515
{
    private readonly FakeClock _relogio = new();

    private ContadorDeFalhasDeLogin NovoContador() => new(_relogio);

    [TestMethod]
    public void QuatroFalhas_NaoBloqueiam_ACincoBloqueia()
    {
        ContadorDeFalhasDeLogin contador = NovoContador();

        for (int i = 0; i < 4; i++)
        {
            Assert.IsFalse(contador.RegistrarFalha("1.1.1.1"), "a falha " + (i + 1) + " ainda não fecha o bloqueio");
            Assert.IsFalse(contador.EstaBloqueado("1.1.1.1"));
        }

        Assert.IsTrue(contador.RegistrarFalha("1.1.1.1"), "a quinta falha fecha o bloqueio");
        Assert.IsTrue(contador.EstaBloqueado("1.1.1.1"));
    }

    [TestMethod]
    public void Bloqueio_AcabaQuandoAFalhaMaisAntigaSaiDaJanela()
    {
        ContadorDeFalhasDeLogin contador = NovoContador();
        for (int i = 0; i < 5; i++)
        {
            contador.RegistrarFalha("1.1.1.1");
        }

        _relogio.Now += TimeSpan.FromMinutes(14);
        Assert.IsTrue(contador.EstaBloqueado("1.1.1.1"));
        Assert.AreEqual(TimeSpan.FromMinutes(1), contador.EsperaRestante("1.1.1.1"));

        _relogio.Now += TimeSpan.FromMinutes(1);
        Assert.IsFalse(contador.EstaBloqueado("1.1.1.1"));
        Assert.AreEqual(TimeSpan.Zero, contador.EsperaRestante("1.1.1.1"));
    }

    [TestMethod]
    public void FalhasEspacadas_ForaDaJanela_NaoSeSomam()
    {
        ContadorDeFalhasDeLogin contador = NovoContador();
        for (int i = 0; i < 4; i++)
        {
            contador.RegistrarFalha("1.1.1.1");
            _relogio.Now += TimeSpan.FromMinutes(4);
        }

        // As duas primeiras (há 16 e 12 minutos) já saíram ou estão no limite: uma nova falha não chega a 5 na janela
        Assert.IsFalse(contador.RegistrarFalha("1.1.1.1"));
        Assert.IsFalse(contador.EstaBloqueado("1.1.1.1"));
    }

    [TestMethod]
    public void EntradaComSucesso_ZeraAContagem()
    {
        ContadorDeFalhasDeLogin contador = NovoContador();
        for (int i = 0; i < 4; i++)
        {
            contador.RegistrarFalha("1.1.1.1");
        }

        contador.Zerar("1.1.1.1");
        for (int i = 0; i < 4; i++)
        {
            Assert.IsFalse(contador.RegistrarFalha("1.1.1.1"));
        }

        Assert.IsFalse(contador.EstaBloqueado("1.1.1.1"), "4 + sucesso + 4 não bloqueia");
    }

    [TestMethod]
    public void OrigensDiferentes_NaoSeAfetam()
    {
        ContadorDeFalhasDeLogin contador = NovoContador();
        for (int i = 0; i < 5; i++)
        {
            contador.RegistrarFalha("1.1.1.1");
        }

        Assert.IsTrue(contador.EstaBloqueado("1.1.1.1"));
        Assert.IsFalse(contador.EstaBloqueado("2.2.2.2"));
    }

    [TestMethod]
    public void EnquantoBloqueado_NovasTentativasNaoPrologamOBloqueio()
    {
        ContadorDeFalhasDeLogin contador = NovoContador();
        for (int i = 0; i < 5; i++)
        {
            contador.RegistrarFalha("1.1.1.1");
        }

        _relogio.Now += TimeSpan.FromMinutes(10);
        Assert.IsFalse(contador.RegistrarFalha("1.1.1.1"));

        _relogio.Now += TimeSpan.FromMinutes(5);
        Assert.IsFalse(contador.EstaBloqueado("1.1.1.1"), "o bloqueio termina 15 minutos depois da primeira falha");
    }
}

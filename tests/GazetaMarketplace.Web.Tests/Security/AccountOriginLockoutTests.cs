using System;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>SC-03: o bloqueio da entrada é por conta e origem; bloqueios seguidos trazem atraso progressivo.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AccountOriginLockoutTests
#pragma warning restore CA1515
{
    private const int Admin = 1;

    private readonly FakeClock _clock = new();

    private AccountOriginLockout New() => new(_clock);

    private static void Block(AccountOriginLockout lockout, int user, string origin)
    {
        for (int i = 0; i < AccountOriginLockout.Limit; i++)
        {
            lockout.RecordFailure(user, origin);
        }
    }

    [TestMethod]
    public void CincoFalhasBloqueiamAquelaOrigemNaquelaConta_ENaoAsOutras()
    {
        AccountOriginLockout lockout = New();

        for (int i = 1; i <= 4; i++)
        {
            Assert.IsFalse(lockout.RecordFailure(Admin, "1.1.1.1").BlockStarted, "a falha " + i);
        }

        Assert.IsFalse(lockout.IsBlocked(Admin, "1.1.1.1"));
        Assert.IsTrue(lockout.RecordFailure(Admin, "1.1.1.1").BlockStarted, "a quinta fecha o bloqueio");
        Assert.IsTrue(lockout.IsBlocked(Admin, "1.1.1.1"));
        Assert.IsFalse(lockout.IsBlocked(Admin, "2.2.2.2"), "outro IP continua entrando na mesma conta: o atacante não trava o Administrador");
        Assert.IsFalse(lockout.IsBlocked(2, "1.1.1.1"), "o mesmo IP continua tentando outra conta (o limite por IP é do LoginFailureCounter)");
    }

    [TestMethod]
    public void Bloqueio_AcabaQuandoAFalhaMaisAntigaSaiDaJanela_ESucessoZera()
    {
        AccountOriginLockout lockout = New();
        Block(lockout, Admin, "1.1.1.1");
        Assert.IsTrue(lockout.RemainingWait(Admin, "1.1.1.1") > TimeSpan.Zero);

        _clock.Now += AccountOriginLockout.Window + TimeSpan.FromSeconds(1);
        Assert.IsFalse(lockout.IsBlocked(Admin, "1.1.1.1"));
        Assert.AreEqual(TimeSpan.Zero, lockout.RemainingWait(Admin, "1.1.1.1"));

        for (int i = 0; i < 3; i++)
        {
            lockout.RecordFailure(Admin, "3.3.3.3");
        }

        lockout.Reset(Admin, "3.3.3.3");
        for (int i = 0; i < 4; i++)
        {
            Assert.IsFalse(lockout.RecordFailure(Admin, "3.3.3.3").BlockStarted, "a contagem recomeçou depois da entrada com sucesso");
        }
    }

    [TestMethod]
    public void SemAtrasoAteOTerceiroBloqueio_Depois1_2_4_8_16_E_NaoPassaDe16()
    {
        AccountOriginLockout lockout = New();
        // índice = número do bloqueio (o 0 não é usado)
        TimeSpan[] expected = [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16)];

        Assert.AreEqual(TimeSpan.Zero, lockout.DelayFor(Admin), "nenhum bloqueio");
        for (int block = 1; block <= 7; block++)
        {
            LockoutOutcome last = default;
            for (int i = 0; i < AccountOriginLockout.Limit; i++)
            {
                last = lockout.RecordFailure(Admin, "10.0.0." + block);
            }

            Assert.IsTrue(last.BlockStarted);
            Assert.AreEqual(block, last.ConsecutiveBlocks);
            Assert.AreEqual(expected[block], lockout.DelayFor(Admin), "atraso depois do bloqueio " + block);
        }
    }

    [TestMethod]
    public void BloqueiosSeguidosEsfriam_ESucessoOuRedefinicaoZeramATodos()
    {
        AccountOriginLockout lockout = New();
        for (int block = 1; block <= 3; block++)
        {
            Block(lockout, Admin, "10.0.0." + block);
        }

        Assert.AreEqual(TimeSpan.FromSeconds(1), lockout.DelayFor(Admin));
        _clock.Now += AccountOriginLockout.BlockMemory + TimeSpan.FromMinutes(1);
        Assert.AreEqual(TimeSpan.Zero, lockout.DelayFor(Admin), "uma hora sem bloqueio: a sequência recomeça");

        for (int block = 1; block <= 3; block++)
        {
            Block(lockout, Admin, "20.0.0." + block);
        }

        lockout.Reset(Admin, "99.9.9.9");
        Assert.AreEqual(TimeSpan.Zero, lockout.DelayFor(Admin), "entrar com sucesso zera a sequência");

        for (int block = 1; block <= 3; block++)
        {
            Block(lockout, Admin, "30.0.0." + block);
        }

        lockout.ClearAccount(Admin);
        Assert.AreEqual(TimeSpan.Zero, lockout.DelayFor(Admin));
        Assert.IsFalse(lockout.IsBlocked(Admin, "30.0.0.1"), "redefinir a senha (RC-12) ou reativar tira todos os bloqueios da conta");
    }

    [TestMethod]
    public void FalhaNumaOrigemJaBloqueada_NaoContaDeNovoNemFazOutroBloqueio()
    {
        AccountOriginLockout lockout = New();
        Block(lockout, Admin, "1.1.1.1");

        LockoutOutcome again = lockout.RecordFailure(Admin, "1.1.1.1");

        Assert.IsFalse(again.BlockStarted);
        Assert.AreEqual(1, again.ConsecutiveBlocks);
    }
}

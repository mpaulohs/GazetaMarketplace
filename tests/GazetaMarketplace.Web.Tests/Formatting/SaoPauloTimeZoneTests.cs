using System;
using GazetaMarketplace.Core.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Formatting;

/// <summary>R-05: o fuso de São Paulo nunca derruba o tipo, nem em Windows sem ICU (só com o id próprio do Windows) nem sem nenhum dos dois.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SaoPauloTimeZoneTests
#pragma warning restore CA1515
{
    private static readonly TimeZoneInfo Marker = TimeZoneInfo.CreateCustomTimeZone("marcador", TimeSpan.FromHours(-3), "marcador", "marcador");

    [TestMethod]
    public void IdIana_E_OPrimeiroTentado()
    {
        string first = null;
        TimeZoneInfo zone = SaoPauloTimeZone.Resolve(id =>
        {
            first ??= id;
            return Marker;
        });

        Assert.AreEqual(SaoPauloTimeZone.IanaId, first);
        Assert.AreSame(Marker, zone);
    }

    [TestMethod]
    public void WindowsSemIcu_UsaOIdProprioDoWindows()
    {
        TimeZoneInfo zone = SaoPauloTimeZone.Resolve(id => id == SaoPauloTimeZone.WindowsId ? Marker : throw new TimeZoneNotFoundException(id));

        Assert.AreSame(Marker, zone);
    }

    [TestMethod]
    [DataRow(typeof(TimeZoneNotFoundException))]
    [DataRow(typeof(InvalidTimeZoneException))]
    public void SemNenhumDosDois_CaiNoFusoFixoDeMenosTresHoras_ENaoLanca(Type error)
    {
        TimeZoneInfo zone = SaoPauloTimeZone.Resolve(id => throw (Exception)Activator.CreateInstance(error, id));

        Assert.AreEqual(SaoPauloTimeZone.FixedOffsetId, zone.Id);
        DateTime utc = new(2026, 10, 7, 2, 30, 0, DateTimeKind.Utc);
        Assert.AreEqual(new DateTime(2026, 10, 6, 23, 30, 0), TimeZoneInfo.ConvertTimeFromUtc(utc, zone), "UTC−03:00 o ano todo: o Brasil não tem horário de verão desde 2019");
        Assert.AreEqual(new DateTime(2026, 1, 15, 9, 0, 0), TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), zone));
    }

    [TestMethod]
    public void ForaDoPadrao_OutraExcecaoNaoEEngolida()
    {
        Assert.Throws<InvalidOperationException>(() => SaoPauloTimeZone.Resolve(_ => throw new InvalidOperationException("inesperada")));
    }

    [TestMethod]
    public void NoServidorDosTestes_ResolveOFusoRealDeSaoPaulo()
    {
        TimeZoneInfo zone = SaoPauloTimeZone.Resolve();

        Assert.AreEqual(TimeSpan.FromHours(-3), zone.GetUtcOffset(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc)));
    }
}

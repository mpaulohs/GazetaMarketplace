using System;
using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Relógio controlável (TimeProvider escrito à mão, sem pacote extra).</summary>
internal sealed class FakeClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Usuário atual controlável; nulo representa uma ação do sistema.</summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    public int? UserId { get; set; }

    public bool IsAdministrator { get; set; }

    public string CorrelationId { get; set; }
}

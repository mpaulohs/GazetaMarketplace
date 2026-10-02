using System;
using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Relógio controlável (TimeProvider escrito à mão, sem pacote extra).</summary>
internal sealed class RelogioFalso : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Agora;
}

/// <summary>Usuário atual controlável; nulo representa uma ação do sistema.</summary>
internal sealed class UsuarioFalso : IUsuarioAtual
{
    public int? UsuarioId { get; set; }

    public string CorrelationId { get; set; }
}

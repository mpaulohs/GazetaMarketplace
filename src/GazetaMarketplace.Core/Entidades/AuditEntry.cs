using System;

namespace GazetaMarketplace.Core.Entidades;

public enum ResultadoDaAuditoria : byte
{
    Sucesso = 1,
    Falha = 2,
    Negado = 3
}

/// <summary>
/// Registro de uma ação sensível (RC-16): quem, o quê, em quem e quando. Só de acréscimo: todas as
/// propriedades são init-only e o <c>SaveChanges</c> recusa alterar ou apagar uma entrada.
/// Nunca grave senha, token ou link de redefinição nos valores anterior e novo.
/// </summary>
public sealed class AuditEntry
{
    public int Id { get; init; }

    public DateTime OccurredAt { get; init; }

    /// <summary>Id de quem agiu; nulo = ação do sistema.</summary>
    public int? ActorId { get; init; }

    public string Action { get; init; }

    public string TargetType { get; init; }

    public string TargetId { get; init; }

    public string PreviousValue { get; init; }

    public string NewValue { get; init; }

    public ResultadoDaAuditoria Result { get; init; }

    /// <summary>Liga a entrada à linha do log da mesma requisição.</summary>
    public string CorrelationId { get; init; }
}

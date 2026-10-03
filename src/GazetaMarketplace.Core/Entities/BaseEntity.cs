using System;

namespace GazetaMarketplace.Core.Entities;

/// <summary>
/// Base das entidades editáveis (ADR-004). O <c>SaveChangesAsync</c> preenche as colunas de auditoria
/// em UTC; <see cref="RowVersion"/> é o token de concorrência otimista (rowversion do SQL Server).
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Id do usuário que criou; nulo = ação do sistema.</summary>
    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Id do usuário da última alteração; nulo = ação do sistema.</summary>
    public int? UpdatedBy { get; set; }

    public byte[] RowVersion { get; set; }
}

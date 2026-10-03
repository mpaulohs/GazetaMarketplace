using System.Collections.Generic;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// Situações do anúncio, como ficam gravadas em <c>Ads.Status</c> (<c>tinyint</c>, ARCHITECTURE §6.2). Constantes e não <c>enum</c>: o valor
/// do banco é o contrato (as consultas Dapper, o <c>CHECK</c> e os índices usam o número) e não pode mudar por reordenar um tipo.
/// </summary>
public static class AdStatus
{
    public const byte Draft = 1;

    public const byte InReview = 2;

    public const byte Published = 3;

    public const byte Rejected = 4;

    public const byte Archived = 5;

    public static IReadOnlyList<byte> All { get; } = [Draft, InReview, Published, Rejected, Archived];

    public static bool IsValid(byte status) => status is >= Draft and <= Archived;

    /// <summary>O nome que a equipe vê na tela (SPEC, Apêndice A).</summary>
    public static string Label(byte status) => status switch
    {
        Draft => "Rascunho",
        InReview => "Em revisão",
        Published => "Publicado",
        Rejected => "Rejeitado",
        Archived => "Arquivado",
        _ => "Desconhecida"
    };
}

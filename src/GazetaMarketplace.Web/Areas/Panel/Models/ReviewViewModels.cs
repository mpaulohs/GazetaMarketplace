using System.Collections.Generic;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Web.Models;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>A fila de revisão (US-010-S01 e S06): as linhas já na ordem do mais antigo ao mais novo.</summary>
public sealed class ReviewQueueViewModel
{
    public IReadOnlyList<ReviewQueueItem> Items { get; init; } = [];

    public int Count => Items.Count;
}

/// <summary>A pré-visualização do anúncio para o Administrador decidir (US-010-S02): a página como o visitante a veria, com a faixa de "ainda não publicado".</summary>
public sealed class ReviewPreviewViewModel
{
    public required int Id { get; init; }

    public required string StatusLabel { get; init; }

    /// <summary>Verdadeiro quando o anúncio está Em revisão: só então a faixa e as ações valem. Em qualquer outra situação a página só informa.</summary>
    public required bool InReview { get; init; }

    public required AdBodyViewModel Body { get; init; }

    /// <summary>Categoria com os ancestrais ("Imóveis › Casas"); nula se a categoria sumiu da árvore.</summary>
    public string CategoryPath { get; init; }

    /// <summary>Verdadeiro quando cidade e UF foram preenchidas à mão (CEP não conferido): o selo informativo da US-008-S14.</summary>
    public bool LocationManual { get; init; }

    public string Cep { get; init; }

    public IReadOnlyList<AdPhotoItem> Photos { get; init; } = [];

    /// <summary>Em Vagas de emprego não há foto: o bloco neutro "Vaga de emprego" mostra as áreas marcadas.</summary>
    public bool IsJob { get; init; }

    public IReadOnlyList<string> JobAreas { get; init; } = [];

    /// <summary>Telefone do site já formatado ("(11) 91234-5678"); nulo quando ainda não foi configurado.</summary>
    public string Phone { get; init; }

    /// <summary>Só os dígitos, para os links "tel:" e do WhatsApp.</summary>
    public string PhoneDigits { get; init; }
}

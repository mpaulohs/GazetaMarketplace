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

    /// <summary>"Anúncio publicado" ou "Anúncio rejeitado", depois de uma decisão; nulo nas demais visitas.</summary>
    public string Message { get; init; }
}

/// <summary>A pré-visualização do anúncio para o Administrador decidir (US-010-S02): a página como o visitante a veria, com a faixa de "ainda não publicado".</summary>
public sealed class ReviewPreviewViewModel
{
    public required int Id { get; init; }

    public required string StatusLabel { get; init; }

    /// <summary>Verdadeiro quando o anúncio está Em revisão: só então a faixa e as ações valem. Em qualquer outra situação a página só informa.</summary>
    public required bool InReview { get; init; }

    /// <summary>"Arquivar" na barra de decisão do anúncio Em revisão (US-011-S05).</summary>
    public TakedownActions Takedown { get; init; } = TakedownActions.None;

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

    /// <summary>O bloco "Fale com a Gazeta" (o mesmo da página pública); nulo quando o telefone do site ainda não foi configurado.</summary>
    public ContactViewModel Contact { get; init; }

    /// <summary>O aviso da última tentativa de decisão (conflito com outro Administrador, telefone do site ausente); nulo se não houve.</summary>
    public ReviewAlert Alert { get; init; }

    /// <summary>O que falta para publicar, quando a tentativa de publicar achou pendências.</summary>
    public IReadOnlyList<AdPending> Pending { get; init; } = [];
}

/// <summary>Qual aviso a pré-visualização mostra depois de uma decisão que não passou.</summary>
public enum ReviewAlertKind
{
    /// <summary>Outra pessoa decidiu antes: a situação mostrada é a verdadeira.</summary>
    Conflict = 1,

    /// <summary>O telefone/WhatsApp do site não foi configurado: leva a "Configurações".</summary>
    PhoneMissing = 2
}

public sealed record ReviewAlert(ReviewAlertKind Kind, string Message);

/// <summary>A página do motivo da rejeição (US-010-S04 e S05). <see cref="Reason"/> volta como foi digitado quando há erro.</summary>
public sealed class RejectViewModel
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public string Reason { get; set; }
}

/// <summary>A página de confirmação de "Publicar" (US-010-S03).</summary>
public sealed record PublishConfirmationViewModel(int Id, string Title);

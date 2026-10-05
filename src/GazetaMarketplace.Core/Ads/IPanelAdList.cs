using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// A consulta que o serviço entrega ao repositório: já com as regras aplicadas (autoria do Redator, arquivados escondidos, termo normalizado, página dentro do
/// intervalo). O repositório só a executa; nada aqui vem direto da URL.
/// </summary>
/// <param name="AuthorId">Só os anúncios deste autor (o Redator); nulo para o Administrador, que vê todos.</param>
/// <param name="Status">Só esta situação; nulo = todas.</param>
/// <param name="ExcludeArchived">Esconde os arquivados (a lista padrão); só é falso quando a situação pedida é "Arquivado".</param>
/// <param name="Search">Termo já normalizado (<see cref="Search.Normalizer"/>); vazio = sem busca.</param>
/// <param name="Page">Página (a partir de 1).</param>
/// <param name="PageSize">Quantos por página.</param>
public sealed record PanelAdListQuery(int? AuthorId, byte? Status, bool ExcludeArchived, string Search, int Page, int PageSize);

/// <summary>Uma linha lida do banco, sem o nome da categoria (o serviço o resolve pela árvore em memória).</summary>
public sealed record PanelAdListRow(int Id, string Title, int AuthorId, string AuthorName, int? CategoryId, byte Status, DateTime ChangedAt, string RejectionReason);

/// <summary>As linhas da página pedida e o total de anúncios que a consulta encontra (todas as páginas).</summary>
public sealed record PanelAdListRows(IReadOnlyList<PanelAdListRow> Rows, int Total);

/// <summary>A leitura da lista do painel (ADR-004): Dapper com parâmetros nomeados, ordem fixa e tempo limite de 10 s.</summary>
public interface IPanelAdListReadRepository
{
    Task<PanelAdListRows> ListAsync(PanelAdListQuery query, CancellationToken cancellationToken);

    /// <summary>Quantos anúncios estão nesta situação, de qualquer autor (a aba "Fila de revisão (n)" do Administrador).</summary>
    Task<int> CountByStatusAsync(byte status, CancellationToken cancellationToken);
}

/// <summary>Uma linha da lista como a tela mostra.</summary>
public sealed record PanelAdListItem(int Id, string Title, string AuthorName, string CategoryName, byte Status, DateTime ChangedAt, string RejectionReason);

/// <summary>Uma página da lista.</summary>
/// <param name="Items">As linhas.</param>
/// <param name="Total">Quantos anúncios a busca encontrou, em todas as páginas.</param>
/// <param name="Page">A página mostrada (já dentro do intervalo).</param>
/// <param name="PageSize">Quantos por página.</param>
/// <param name="Search">O termo como a pessoa digitou (aparado e limitado a 100 caracteres), para devolver ao campo.</param>
/// <param name="Status">A situação filtrada, ou nulo para "todas (exceto arquivados)".</param>
public sealed record PanelAdListPage(IReadOnlyList<PanelAdListItem> Items, int Total, int Page, int PageSize, string Search, byte? Status)
{
    public int TotalPages => Total == 0 ? 1 : (Total + PageSize - 1) / PageSize;

    public bool HasFilters => Status is not null || !string.IsNullOrEmpty(Search);
}

/// <summary>
/// A lista de anúncios do painel (US-012): "Meus anúncios" do Redator e "Anúncios" do Administrador. Quem é o usuário vem da sessão, não do pedido: o Redator só recebe
/// os próprios anúncios, em qualquer situação, e o Administrador todos (NFR-13).
/// </summary>
public interface IPanelAdList
{
    /// <param name="search">Texto da busca pelo título, como digitado.</param>
    /// <param name="situation">A situação em texto de URL (<see cref="PanelAdListFilters.StatusSlug"/>); vazia ou desconhecida = todas.</param>
    /// <param name="page">Página pedida; fora do intervalo vira a primeira ou a última.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    Task<PanelAdListPage> ListAsync(string search, string situation, int page, CancellationToken cancellationToken);

    /// <summary>O total da fila de revisão (só o Administrador usa).</summary>
    Task<int> CountInReviewAsync(CancellationToken cancellationToken);
}

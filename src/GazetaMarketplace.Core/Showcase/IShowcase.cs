using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;

namespace GazetaMarketplace.Core.Showcase;

/// <summary>Os números da vitrine pública (US-001) num lugar só.</summary>
public static class ShowcaseFilters
{
    /// <summary>Anúncios mais recentes da página inicial: 12 preenchem linhas completas em telas de 2, 3 e 4 colunas (SPEC, US-001).</summary>
    public const int RecentCount = 12;

    /// <summary>Anúncios por página na categoria: 24 também fecham linhas completas em 2, 3 e 4 colunas (decisão do Product Owner, 2026-10-05).</summary>
    public const int PageSize = 24;

    /// <summary>
    /// Maior número de página aceito no endereço (<c>?pagina=</c>). Passou disso vale este limite, e a categoria mostra a última página que existe; sem o limite,
    /// um número enorme estouraria a conta do deslocamento na consulta (mesma regra da lista do painel).
    /// </summary>
    public const int MaxPage = 100_000;
}

/// <summary>Um anúncio publicado como a consulta o entrega: tudo o que o card precisa, com os atributos ainda em JSON (o serviço traduz).</summary>
public sealed record ShowcaseRow(int Id, string Title, int? CategoryId, long? PriceCents, string Attributes, string City, string Uf, int? CoverPhotoId, int? CoverWidth, int? CoverHeight);

public sealed record ShowcaseRows(IReadOnlyList<ShowcaseRow> Rows, int Total);

/// <summary>Leitura dos anúncios <b>publicados</b> para a vitrine. Nunca devolve outra situação nem dado do autor.</summary>
public interface IShowcaseReadRepository
{
    /// <summary>Os publicados mais recentes (data de publicação, do mais novo ao mais antigo), no máximo <paramref name="take"/>.</summary>
    Task<IReadOnlyList<ShowcaseRow>> RecentAsync(int take, CancellationToken cancellationToken);

    /// <summary>Os publicados de qualquer uma das categorias, do mais novo ao mais antigo, com o total para a paginação.</summary>
    Task<ShowcaseRows> ByCategoryAsync(IReadOnlyList<int> categoryIds, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Os publicados entre os ids pedidos, em qualquer ordem (o serviço põe na ordem pedida). Id de anúncio que não existe ou que não está publicado simplesmente não volta.</summary>
    Task<IReadOnlyList<ShowcaseRow>> ByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken);
}

/// <summary>Um card de anúncio com o nome da categoria dele (a API de favoritos devolve os dois).</summary>
public sealed record ShowcaseAdCard(AdCardModel Card, string CategoryName);

/// <summary>A página inicial: as categorias principais e os anúncios publicados mais recentes.</summary>
public sealed record ShowcaseHome(IReadOnlyList<CategoryNode> Roots, IReadOnlyList<AdCardModel> Recent);

/// <summary>
/// A página de uma categoria. <see cref="Category"/> é nulo quando o endereço não é de nenhuma categoria (<see cref="Roots"/> vem preenchida mesmo assim, para a página oferecer
/// o caminho de volta). <see cref="Path"/> vai da categoria principal até a própria (no máximo 3 níveis).
/// </summary>
public sealed record ShowcaseCategoryPage(
    CategoryNode Category,
    IReadOnlyList<CategoryNode> Path,
    IReadOnlyList<CategoryNode> Children,
    IReadOnlyList<CategoryNode> Roots,
    IReadOnlyList<AdCardModel> Ads,
    int Total,
    int Page,
    int PageSize)
{
    public bool Found => Category is not null;

    public int TotalPages => Total == 0 ? 1 : (Total + PageSize - 1) / PageSize;
}

/// <summary>Leitura de um anúncio para a página pública. Só devolve o que o público pode ver: anúncio <b>Publicado</b>.</summary>
public interface IPublishedAdReader
{
    /// <summary>O anúncio publicado; nulo se não existe ou está em qualquer outra situação (rascunho, em revisão, rejeitado, arquivado).</summary>
    Task<Ad> FindPublishedAsync(int id, CancellationToken cancellationToken);

    /// <summary>A categoria de um anúncio <b>arquivado</b> (para o link "ver mais anúncios de…" da página de indisponível, US-003-S06); nulo em qualquer outro caso.</summary>
    Task<int?> FindArchivedCategoryIdAsync(int id, CancellationToken cancellationToken);
}

/// <summary>
/// O que a página do anúncio recebe: o anúncio publicado, ou nulo quando ele não está disponível. <see cref="ArchivedCategory"/> só vem preenchida quando o anúncio foi
/// arquivado e a categoria dele ainda existe (S06 pede o link para ela); nos demais casos a resposta é a mesma, sem pista de que o anúncio existe.
/// </summary>
public sealed record ShowcaseAdPage(Ad Ad, CategoryNode ArchivedCategory, IReadOnlyList<CategoryNode> Roots)
{
    public bool Available => Ad is not null;
}

public interface IShowcase
{
    Task<ShowcaseAdPage> AdAsync(int id, CancellationToken cancellationToken);

    Task<ShowcaseHome> HomeAsync(CancellationToken cancellationToken);

    Task<ShowcaseCategoryPage> CategoryAsync(string slug, int page, CancellationToken cancellationToken);

    /// <summary>
    /// Os anúncios <b>publicados</b> entre os ids, <b>na ordem em que foram pedidos</b> (US-005, "Meus favoritos"). Os ids de anúncios despublicados, arquivados ou inexistentes não voltam:
    /// quem chamou compara a lista e tira os ausentes dos favoritos. No máximo <see cref="FavoriteIds.MaxPerRequest"/> ids por chamada.
    /// </summary>
    Task<IReadOnlyList<ShowcaseAdCard>> CardsByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken);
}

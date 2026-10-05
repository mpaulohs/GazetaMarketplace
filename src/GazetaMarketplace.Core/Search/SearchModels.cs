using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Core.VehicleCatalog;

namespace GazetaMarketplace.Core.Search;

/// <summary>Os números e os nomes da busca pública (US-002) num lugar só.</summary>
public static class SearchLimits
{
    /// <summary>Anúncios por página: os mesmos 24 da categoria (fecham linhas completas de 2, 3 e 4 colunas).</summary>
    public const int PageSize = ShowcaseFilters.PageSize;

    /// <summary>Maior termo de busca aceito (RC-15); passou disso a mensagem aparece junto do campo e a lista sai sem o texto.</summary>
    public const int MaxTermLength = 100;

    /// <summary>Quantas palavras do termo contam; as demais são ignoradas.</summary>
    public const int MaxWords = 5;
}

/// <summary>As três ordenações da busca (US-002). Só estas existem: nenhum texto do pedido vira ordenação no SQL.</summary>
public enum SearchOrder
{
    /// <summary>Mais recentes: data de publicação, do mais novo ao mais antigo (o padrão).</summary>
    Recent = 0,

    /// <summary>Menor preço; anúncio sem preço (Serviços, A6) vai para o fim.</summary>
    PriceAscending = 1,

    /// <summary>Maior preço; anúncio sem preço (Serviços, A6) vai para o fim.</summary>
    PriceDescending = 2
}

/// <summary>Os nomes da ordenação no endereço (<c>?ordem=</c>) e na tela.</summary>
public static class SearchOrders
{
    public const string RecentSlug = "recentes";

    public const string PriceAscendingSlug = "menor-preco";

    public const string PriceDescendingSlug = "maior-preco";

    public static IReadOnlyList<SearchOrder> All { get; } = [SearchOrder.Recent, SearchOrder.PriceAscending, SearchOrder.PriceDescending];

    /// <summary>A ordenação do endereço; texto desconhecido ou ausente vira <see cref="SearchOrder.Recent"/> (parâmetro inválido volta ao padrão, sem erro).</summary>
    public static SearchOrder Parse(string slug) => slug?.Trim() switch
    {
        PriceAscendingSlug => SearchOrder.PriceAscending,
        PriceDescendingSlug => SearchOrder.PriceDescending,
        _ => SearchOrder.Recent
    };

    public static string Slug(SearchOrder order) => order switch
    {
        SearchOrder.PriceAscending => PriceAscendingSlug,
        SearchOrder.PriceDescending => PriceDescendingSlug,
        _ => RecentSlug
    };

    public static string Label(SearchOrder order) => order switch
    {
        SearchOrder.PriceAscending => "Menor preço",
        SearchOrder.PriceDescending => "Maior preço",
        _ => "Mais recentes"
    };
}

/// <summary>O que a consulta recebe, já validado e normalizado: cada campo vazio (nulo ou lista vazia) é filtro que não vale.</summary>
public sealed record SearchCriteria
{
    /// <summary>Palavras normalizadas (sem acento, minúsculas); cada uma precisa aparecer no título ou na descrição.</summary>
    public IReadOnlyList<string> Words { get; init; } = [];

    /// <summary>A categoria escolhida e todas as descendentes; vazia = qualquer categoria.</summary>
    public IReadOnlyList<int> CategoryIds { get; init; } = [];

    public string Uf { get; init; }

    public string City { get; init; }

    public long? PriceMinCents { get; init; }

    public long? PriceMaxCents { get; init; }

    public int? BrandId { get; init; }

    public int? ModelId { get; init; }

    public int? YearFrom { get; init; }

    public int? YearTo { get; init; }

    public int? KmMax { get; init; }

    public decimal? AreaMin { get; init; }

    public decimal? AreaMax { get; init; }

    public SearchOrder Order { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = SearchLimits.PageSize;
}

/// <summary>Leitura dos anúncios <b>publicados</b> que atendem à busca. Nunca devolve outra situação nem dado do autor.</summary>
public interface ISearchReadRepository
{
    /// <summary>A página pedida (na ordem pedida, com desempate estável pelo id) e o total de anúncios que atendem a todos os filtros.</summary>
    Task<ShowcaseRows> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken);
}

/// <summary>O que o endereço da busca traz, como texto cru (nada aqui é confiável até <see cref="ISearch.PrepareAsync"/>).</summary>
public sealed record SearchInput(
    string Q,
    string Categoria,
    string Uf,
    string Cidade,
    string PrecoMin,
    string PrecoMax,
    string Marca,
    string Modelo,
    string AnoDe,
    string AnoAte,
    string KmMax,
    string AreaMin,
    string AreaMax,
    string Ordem,
    string Pagina);

/// <summary>Uma categoria da lista de escolha, com o que a escolha dela liga na tela: os filtros específicos e o tipo de catálogo (<c>car</c> ou <c>moto</c>).</summary>
public sealed record SearchCategoryOption(string Slug, string Name, int Depth, SearchFilter Filters, string Kind);

/// <summary>
/// A tela de busca pronta para desenhar: o que foi pedido (com o texto cru dos campos, para mostrar o que a pessoa digitou), o que vale de fato (<see cref="Criteria"/>),
/// os erros junto de cada campo e as listas das caixas de escolha.
/// </summary>
public sealed class SearchForm
{
    public string Text { get; init; } = string.Empty;

    public string CategorySlug { get; init; }

    public string Uf { get; init; }

    public string City { get; init; }

    public string PriceMin { get; init; }

    public string PriceMax { get; init; }

    public string Brand { get; init; }

    public string Model { get; init; }

    public string YearFrom { get; init; }

    public string YearTo { get; init; }

    public string KmMax { get; init; }

    public string AreaMin { get; init; }

    public string AreaMax { get; init; }

    public SearchOrder Order { get; init; }

    public int Page { get; init; } = 1;

    /// <summary>Mensagem junto do campo, por nome do parâmetro (<c>q</c>, <c>preco</c>, <c>precoMin</c>, <c>precoMax</c>, <c>ano</c>, <c>anoDe</c>, <c>anoAte</c>, <c>kmMax</c>, <c>area</c>, <c>areaMin</c>, <c>areaMax</c>).</summary>
    public IReadOnlyDictionary<string, string> Errors { get; init; } = new Dictionary<string, string>();

    public required SearchCriteria Criteria { get; init; }

    public CategoryNode Category { get; init; }

    public IReadOnlyList<CategoryNode> Path { get; init; } = [];

    public IReadOnlyList<CategoryNode> Roots { get; init; } = [];

    public IReadOnlyList<SearchCategoryOption> CategoryOptions { get; init; } = [];

    /// <summary>Os filtros específicos que a categoria escolhida oferece (marca, modelo, ano, quilometragem, área).</summary>
    public SearchFilter Filters { get; init; }

    /// <summary>O tipo de catálogo da categoria escolhida (<c>car</c> ou <c>moto</c>); nulo fora de Carros e Motos.</summary>
    public string Kind { get; init; }

    public IReadOnlyList<CatalogItem> Brands { get; init; } = [];

    public IReadOnlyList<CatalogItem> Models { get; init; } = [];

    /// <summary>As cidades da UF escolhida (lista do IBGE); vazia sem UF.</summary>
    public IReadOnlyList<CityItem> Cities { get; init; } = [];

    /// <summary>Só os filtros válidos, no endereço: é o que os links de página e o "Ordenar" repetem (sem <c>pagina</c>).</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Query { get; init; } = [];

    /// <summary>Há algum filtro valendo (texto, categoria, local, preço, característica ou ordem)?</summary>
    public bool IsFiltered => Query.Count > 0;

    public bool HasErrors => Errors.Count > 0;
}

/// <summary>Uma página de resultados: os cards, o total e a página mostrada.</summary>
public sealed record SearchResult(IReadOnlyList<AdCardModel> Ads, int Total, int Page, int PageSize)
{
    public int TotalPages => Total == 0 ? 1 : (Total + PageSize - 1) / PageSize;
}

/// <summary>A busca pública (US-002) em dois passos: entender o pedido e consultar. Falhar na consulta ainda deixa o formulário pronto para a tela mostrar os filtros preservados.</summary>
public interface ISearch
{
    /// <summary>Valida e normaliza o pedido: o que é inválido volta ao padrão e, nos três casos com mensagem, gera o erro do campo.</summary>
    Task<SearchForm> PrepareAsync(SearchInput input, CancellationToken cancellationToken);

    /// <summary>Consulta os anúncios publicados que atendem ao formulário.</summary>
    Task<SearchResult> RunAsync(SearchForm form, CancellationToken cancellationToken);
}

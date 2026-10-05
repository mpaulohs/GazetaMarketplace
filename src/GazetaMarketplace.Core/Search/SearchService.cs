using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Core.VehicleCatalog;

namespace GazetaMarketplace.Core.Search;

/// <inheritdoc cref="ISearch"/>
/// <remarks>
/// Parâmetro inválido volta ao padrão, sem erro (página, ordem, categoria que não existe, UF desconhecida, cidade que não é da UF, marca ou modelo fora do catálogo). Só
/// quatro situações geram mensagem junto do campo e, nelas, a lista sai <b>sem</b> o filtro com erro: texto com mais de 100 caracteres, valor ilegível, faixa invertida (preço,
/// ano ou área) e ano fora do intervalo. O que vale de fato fica em <see cref="SearchForm.Criteria"/>; o que a pessoa digitou fica nos campos de texto do formulário.
/// </remarks>
public sealed class SearchService(
    ISearchReadRepository repository,
    ICategoryTree tree,
    ICityDirectory cities,
    IVehicleCatalog catalog,
    TimeProvider time) : ISearch
{
    public const string PriceFormatMessage = "Informe um valor em reais, como 50000 ou 50.000,00";
    public const string PriceRangeMessage = "O preço mínimo não pode ser maior que o máximo";
    public const string TermTooLongMessage = "O texto da busca pode ter no máximo 100 caracteres";
    public const string KmMessage = "Informe a quilometragem só com números, de 0 a 9.999.999";
    public const string AreaFormatMessage = "Informe a área em m², como 300 ou 300,5";
    public const string AreaRangeMessage = "A área mínima não pode ser maior que a máxima";
    public const string YearRangeMessage = "O ano inicial não pode ser maior que o final";

    private static string YearFormatMessage(int max) => $"Informe um ano de {ModelYearRules.MinYear} a {max}";

    public async Task<SearchForm> PrepareAsync(SearchInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        Dictionary<string, string> errors = [];
        List<KeyValuePair<string, string>> query = [];

        // Texto: até 100 caracteres; cada palavra (no máximo 5) precisa aparecer no título ou na descrição, sem acento e sem diferença de maiúscula
        string text = (input.Q ?? string.Empty).Trim();
        string[] words = [];
        if (text.Length > SearchLimits.MaxTermLength)
        {
            errors["q"] = TermTooLongMessage;
        }
        else if (text.Length > 0)
        {
            words = [.. Normalizer.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).Take(SearchLimits.MaxWords)];
            query.Add(new("q", text));
        }

        // Categoria: pelo slug; a principal inclui todas as descendentes. Slug que não existe volta a "todas as categorias"
        CategoryNode category = string.IsNullOrWhiteSpace(input.Categoria) ? null : snapshot.FindBySlug(input.Categoria.Trim());
        FieldGroup group = category is null ? null : FieldGroupRegistry.Resolve(snapshot, category.Id);
        SearchFilter filters = category is null ? SearchFilter.None : SearchFilters.For(group, category.Id);
        string kind = SearchFilters.KindOf(group);
        int[] categoryIds = category is null ? [] : [category.Id, .. snapshot.DescendantsOf(category.Id).Select(c => c.Id)];
        if (category is not null)
        {
            query.Add(new("categoria", category.Slug));
        }

        // Local: a cidade só existe depois da UF e sempre pertence a ela (lista do IBGE; UF sem carga não filtra por cidade)
        string uf = BrazilianStates.Find(input.Uf)?.Uf;
        IReadOnlyList<CityItem> cityList = [];
        string city = null;
        if (uf is not null)
        {
            query.Add(new("uf", uf));
            cityList = await cities.ByUfAsync(uf, cancellationToken);
            string wanted = Normalizer.Normalize(input.Cidade);
            city = wanted.Length == 0 ? null : cityList.FirstOrDefault(c => Normalizer.Normalize(c.Name) == wanted)?.Name;
            if (city is not null)
            {
                query.Add(new("cidade", city));
            }
        }

        // Preço: reais inteiros ou com vírgula; ilegível ou invertido mostra o erro junto do campo e a lista sai sem a faixa
        (long? priceMin, long? priceMax) = ParsePriceRange(input.PrecoMin, input.PrecoMax, errors, query);

        // Características do bem: só as que o grupo da categoria escolhida oferece; o resto do endereço é ignorado
        IReadOnlyList<CatalogItem> brands = [];
        IReadOnlyList<CatalogItem> models = [];
        int? brandId = null;
        int? modelId = null;
        if (kind is not null && filters.HasFlag(SearchFilter.Brand))
        {
            brands = await catalog.BrandsAsync(kind, cancellationToken);
            brandId = ParseId(input.Marca, brands);
            if (brandId is { } brand && filters.HasFlag(SearchFilter.Model))
            {
                models = await ModelsOrEmptyAsync(kind, brand, cancellationToken);
                modelId = ParseId(input.Modelo, models);
            }

            if (brandId is not null)
            {
                query.Add(new("marca", brandId.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (modelId is not null)
            {
                query.Add(new("modelo", modelId.Value.ToString(CultureInfo.InvariantCulture)));
            }
        }

        (int? yearFrom, int? yearTo) = filters.HasFlag(SearchFilter.Year) ? ParseYears(input.AnoDe, input.AnoAte, errors, query) : (null, null);
        int? kmMax = filters.HasFlag(SearchFilter.Km) ? ParseKm(input.KmMax, errors, query) : null;
        (decimal? areaMin, decimal? areaMax) = filters.HasFlag(SearchFilter.Area) ? ParseAreas(input.AreaMin, input.AreaMax, errors, query) : (null, null);

        SearchOrder order = SearchOrders.Parse(input.Ordem);
        if (order != SearchOrder.Recent)
        {
            query.Add(new("ordem", SearchOrders.Slug(order)));
        }

        int page = int.TryParse(input.Pagina, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedPage) ? Math.Clamp(parsedPage, 1, ShowcaseFilters.MaxPage) : 1;

        return new SearchForm
        {
            Text = text,
            CategorySlug = category?.Slug,
            Uf = uf,
            City = city,
            PriceMin = Clean(input.PrecoMin),
            PriceMax = Clean(input.PrecoMax),
            Brand = brandId?.ToString(CultureInfo.InvariantCulture),
            Model = modelId?.ToString(CultureInfo.InvariantCulture),
            YearFrom = filters.HasFlag(SearchFilter.Year) ? Clean(input.AnoDe) : null,
            YearTo = filters.HasFlag(SearchFilter.Year) ? Clean(input.AnoAte) : null,
            KmMax = filters.HasFlag(SearchFilter.Km) ? Clean(input.KmMax) : null,
            AreaMin = filters.HasFlag(SearchFilter.Area) ? Clean(input.AreaMin) : null,
            AreaMax = filters.HasFlag(SearchFilter.Area) ? Clean(input.AreaMax) : null,
            Order = order,
            Page = page,
            Errors = errors,
            Category = category,
            Path = category is null ? [] : snapshot.PathTo(category.Id),
            Roots = snapshot.Roots,
            CategoryOptions = [.. snapshot.All.Select(node => OptionOf(snapshot, node))],
            Filters = filters,
            Kind = kind,
            Brands = brands,
            Models = models,
            Cities = cityList,
            Query = query,
            Criteria = new SearchCriteria
            {
                Words = words,
                CategoryIds = categoryIds,
                Uf = uf,
                City = city,
                PriceMinCents = priceMin,
                PriceMaxCents = priceMax,
                BrandId = brandId,
                ModelId = modelId,
                YearFrom = yearFrom,
                YearTo = yearTo,
                KmMax = kmMax,
                AreaMin = areaMin,
                AreaMax = areaMax,
                Order = order,
                Page = page,
                PageSize = SearchLimits.PageSize
            }
        };
    }

    public async Task<SearchResult> RunAsync(SearchForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        SearchCriteria criteria = form.Criteria;
        ShowcaseRows result = await repository.SearchAsync(criteria, cancellationToken);
        if (result.Rows.Count == 0 && result.Total > 0 && criteria.Page > 1)
        {
            // Página além do fim (endereço digitado à mão): mostra a última
            criteria = criteria with { Page = (result.Total + criteria.PageSize - 1) / criteria.PageSize };
            result = await repository.SearchAsync(criteria, cancellationToken);
        }

        return new SearchResult([.. result.Rows.Select(row => ShowcaseCards.Create(row, snapshot))], result.Total, criteria.Page, criteria.PageSize);
    }

    private static SearchCategoryOption OptionOf(CategoryTreeSnapshot snapshot, CategoryNode node)
    {
        FieldGroup group = FieldGroupRegistry.Resolve(snapshot, node.Id);
        return new SearchCategoryOption(node.Slug, node.Name, node.Depth, SearchFilters.For(group, node.Id), SearchFilters.KindOf(group));
    }

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ParseId(string value, IReadOnlyList<CatalogItem> allowed) =>
        int.TryParse(value?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int id) && allowed.Any(item => item.Id == id) ? id : null;

    private async Task<IReadOnlyList<CatalogItem>> ModelsOrEmptyAsync(string kind, int brandId, CancellationToken cancellationToken)
    {
        try
        {
            return await catalog.ModelsAsync(kind, brandId, cancellationToken);
        }
        catch (NotFoundException)
        {
            return [];
        }
    }

    private static (long? Min, long? Max) ParsePriceRange(string rawMin, string rawMax, Dictionary<string, string> errors, List<KeyValuePair<string, string>> query)
    {
        long? min = ParseCents(rawMin, "precoMin", errors);
        long? max = ParseCents(rawMax, "precoMax", errors);
        if (min is { } low && max is { } high && low > high)
        {
            errors["preco"] = PriceRangeMessage;
            return (null, null);
        }

        if (min is not null)
        {
            query.Add(new("precoMin", rawMin.Trim()));
        }

        if (max is not null)
        {
            query.Add(new("precoMax", rawMax.Trim()));
        }

        return (min, max);
    }

    private static long? ParseCents(string raw, string field, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DecimalInput.TryParseCents(raw, FieldLimits.MaxMoneyCents, out long cents))
        {
            return cents;
        }

        errors[field] = PriceFormatMessage;
        return null;
    }

    private (int? From, int? To) ParseYears(string rawFrom, string rawTo, Dictionary<string, string> errors, List<KeyValuePair<string, string>> query)
    {
        int max = ModelYearRules.MaxYear(ModelYearRules.CurrentYear(time));
        int? from = ParseYear(rawFrom, "anoDe", max, errors);
        int? to = ParseYear(rawTo, "anoAte", max, errors);
        if (from is { } low && to is { } high && low > high)
        {
            errors["ano"] = YearRangeMessage;
            return (null, null);
        }

        if (from is not null)
        {
            query.Add(new("anoDe", from.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (to is not null)
        {
            query.Add(new("anoAte", to.Value.ToString(CultureInfo.InvariantCulture)));
        }

        return (from, to);
    }

    private static int? ParseYear(string raw, string field, int max, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (int.TryParse(raw.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int year) && year >= ModelYearRules.MinYear && year <= max)
        {
            return year;
        }

        errors[field] = YearFormatMessage(max);
        return null;
    }

    private static int? ParseKm(string raw, Dictionary<string, string> errors, List<KeyValuePair<string, string>> query)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (int.TryParse(raw.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int km) && km <= FieldLimits.MaxKm)
        {
            query.Add(new("kmMax", km.ToString(CultureInfo.InvariantCulture)));
            return km;
        }

        errors["kmMax"] = KmMessage;
        return null;
    }

    private static (decimal? Min, decimal? Max) ParseAreas(string rawMin, string rawMax, Dictionary<string, string> errors, List<KeyValuePair<string, string>> query)
    {
        decimal? min = ParseArea(rawMin, "areaMin", errors);
        decimal? max = ParseArea(rawMax, "areaMax", errors);
        if (min is { } low && max is { } high && low > high)
        {
            errors["area"] = AreaRangeMessage;
            return (null, null);
        }

        if (min is not null)
        {
            query.Add(new("areaMin", rawMin.Trim()));
        }

        if (max is not null)
        {
            query.Add(new("areaMax", rawMax.Trim()));
        }

        return (min, max);
    }

    private static decimal? ParseArea(string raw, string field, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DecimalInput.TryParse(raw, out decimal area) && area <= FieldLimits.MaxAreaM2)
        {
            return area;
        }

        errors[field] = AreaFormatMessage;
        return null;
    }
}

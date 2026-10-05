using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Formatting;
using GazetaMarketplace.Core.Photos;

namespace GazetaMarketplace.Web.Models;

/// <summary>Tudo o que a tela do anúncio mostra, já montado: o corpo, as fotos, o bloco da vaga, e o caminho de categorias.</summary>
/// <param name="Body">Título, valor, local, descrição e características.</param>
/// <param name="Photos">As fotos na ordem da galeria; vazia em Vagas de emprego (não têm foto).</param>
/// <param name="IsJob">Vaga de emprego: no lugar da galeria vai o bloco "Vaga de emprego" com as áreas.</param>
/// <param name="JobAreas">As áreas marcadas da vaga, uma a uma.</param>
/// <param name="Path">Da categoria principal até a do anúncio; vazia se a categoria sumiu da árvore.</param>
/// <param name="GroupKey">Chave do grupo de campos da categoria.</param>
public sealed record AdDetail(AdBodyViewModel Body, IReadOnlyList<AdPhotoItem> Photos, bool IsJob, IReadOnlyList<string> JobAreas, IReadOnlyList<CategoryNode> Path, string GroupKey)
{
    /// <summary>"Imóveis › Casas"; nulo quando a categoria sumiu.</summary>
    public string CategoryPath => Path.Count == 0 ? null : string.Join(" › ", Path.Select(n => n.Name));
}

/// <summary>
/// Monta o <see cref="AdDetail"/> de um anúncio: o <b>único</b> lugar que decide o grupo de campos, o valor (Preço, Salário ou Tipo do serviço), as características, o título do
/// bloco delas, as fotos e as áreas da vaga. A pré-visualização do Administrador e a página pública do anúncio usam a mesma montagem, então o que o Administrador aprova é o que o visitante vê.
/// </summary>
public sealed class AdDetailFactory(ICategoryTree tree, IAdSpecsReader specs, IAdPhotoService photos)
{
    /// <param name="ad">O anúncio, já lido (quem chama decidiu se pode mostrá-lo).</param>
    /// <param name="headingLevel">Nível do título (1 na página do anúncio; 2 dentro da pré-visualização).</param>
    /// <param name="includePublicMeta">Verdadeiro na página pública: o corpo traz o nome da categoria e a data de publicação.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    public async Task<AdDetail> CreateAsync(Ad ad, int headingLevel, bool includePublicMeta, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ad);

        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        int? categoryId = ad.CategoryId is { } c && snapshot.Find(c) is not null ? c : null;
        FieldGroup group = categoryId is { } known ? FieldGroupRegistry.Resolve(snapshot, known) : FieldGroupRegistry.Default;
        bool isJob = group.Key == FieldGroupKeys.Jobs;

        IReadOnlyList<AdSpec> characteristics = categoryId is { } specsCategory ? await specs.ReadAsync(ad, specsCategory, group, cancellationToken) : [];
        IReadOnlyList<AdPhotoItem> gallery = isJob ? [] : await photos.ListAsync(ad.Id, cancellationToken);
        IReadOnlyList<CategoryNode> path = categoryId is { } pathCategory ? snapshot.PathTo(pathCategory) : [];

        AdBodyViewModel body = new()
        {
            Title = ad.Title,
            Value = AdPresentation.ValueOf(group, ad.PriceCents, group.HasPrice ? null : characteristics.FirstOrDefault(s => s.Key == FieldKeys.ServiceType)?.Value),
            Location = AdPresentation.Location(ad.City, ad.Uf),
            DescriptionLabel = group.DescriptionLabel,
            Description = ad.Description,
            Specs = isJob ? [.. characteristics.Where(s => s.Key != FieldKeys.JobAreas)] : characteristics,
            SpecsTitle = AdSpecTitles.For(group.Key, categoryId),
            CategoryName = includePublicMeta ? path.LastOrDefault()?.Name : null,
            PublishedOn = includePublicMeta && ad.PublishedAt is { } published ? CurrencyAndDateFormatter.FormatDate(published) : null,
            HeadingLevel = headingLevel
        };

        IReadOnlyList<string> areas = isJob ? [.. characteristics.Where(s => s.Key == FieldKeys.JobAreas).SelectMany(s => s.Items ?? [])] : [];
        return new AdDetail(body, gallery, isJob, areas, path, group.Key);
    }
}

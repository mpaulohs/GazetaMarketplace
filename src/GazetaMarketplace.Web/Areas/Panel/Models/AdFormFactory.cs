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
using GazetaMarketplace.Core.Formatting;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Core.VehicleCatalog;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>
/// Monta o formulário do anúncio a partir do anúncio gravado ou do que foi digitado. As listas (categorias, catálogo de veículos, cidades) vêm do
/// servidor, então a página funciona sem JavaScript; o JavaScript só as troca na hora, sem recarregar.
/// </summary>
public sealed class AdFormFactory(ICategoryTree tree, IVehicleCatalog catalog, ICityDirectory cities, IAdPhotoService photos, TimeProvider time, ICurrentUser currentUser)
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>Os valores gravados do anúncio, como texto do formulário.</summary>
    public async Task<AdFormSubmission> FromAdAsync(Ad ad, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ad);
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        AdFormSubmission values = new()
        {
            Title = ad.Title,
            Description = ad.Description,
            CategoryId = ad.CategoryId,
            Price = ad.PriceCents is { } cents ? PriceText.Format(cents) : null,
            Cep = ad.Cep,
            City = ad.City,
            Uf = ad.Uf,
            LocationCep = string.IsNullOrEmpty(ad.City) ? null : ad.Cep,
            LocationManual = ad.LocationManual,
            RowVersion = ad.RowVersion is { Length: > 0 } ? Convert.ToBase64String(ad.RowVersion) : null
        };

        if (ad.CategoryId is { } category && snapshot.Find(category) is not null && AdAttributes.TryParse(ad.Attributes, out AdAttributes attributes))
        {
            FieldGroup group = FieldGroupRegistry.Resolve(snapshot, category);
            foreach (FieldDefinition field in group.FieldsFor(category))
            {
                string[] raw = ReadStored(field, attributes);
                if (raw.Length > 0)
                {
                    values.Fields[field.Key] = raw;
                }
            }
        }

        return values;
    }

    /// <summary>Monta a tela. <paramref name="ad"/> é nulo no anúncio novo.</summary>
    public async Task<AdFormViewModel> BuildAsync(
        Ad ad, AdFormSubmission values, bool readOnly, string readOnlyMessage, string message, string warning, bool forceManual, CancellationToken cancellationToken,
        IReadOnlyList<AdPending> pendings = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        int? categoryId = values.CategoryId is { } c && snapshot.Find(c) is { IsPostable: true } ? c : null;
        FieldGroup group = categoryId is { } id ? FieldGroupRegistry.Resolve(snapshot, id) : FieldGroupRegistry.Default;

        bool manual = values.LocationManual || forceManual;
        string uf = BrazilianStates.Find(values.Uf)?.Uf ?? values.Uf;
        IReadOnlyList<string> cityOptions = manual && BrazilianStates.IsValid(uf)
            ? [.. (await cities.ByUfAsync(uf, cancellationToken)).Select(city => city.Name)]
            : [];

        // O limite de fotos é o da categoria já gravada (é o que o servidor aplica ao enviar), não o da que está escolhida no formulário e ainda não foi salva
        FieldGroup photoGroup = ad?.CategoryId is { } savedCategory
            ? FieldGroupRegistry.Resolve(snapshot, savedCategory) ?? FieldGroupRegistry.Default
            : FieldGroupRegistry.Default;

        return new AdFormViewModel
        {
            Id = ad?.Id,
            RowVersion = values.RowVersion,
            Status = ad?.Status ?? AdStatus.Draft,
            StatusLabel = ad is null ? null : AdStatus.Label(ad.Status),
            RejectionReason = ad is { Status: AdStatus.Rejected } ? ad.RejectionReason : null,
            ReadOnly = readOnly,
            ReadOnlyMessage = readOnlyMessage,
            Title = values.Title,
            Description = values.Description,
            CategoryId = categoryId,
            CategoryName = categoryId is { } named ? snapshot.Find(named)?.Name : null,
            Price = values.Price,
            Cep = FormatCep(values.Cep),
            City = values.City,
            Uf = uf,
            LocationCep = values.LocationCep,
            LocationManual = manual,
            CityOptions = cityOptions,
            Group = group,
            Fields = categoryId is { } fieldsCategory ? await BuildFieldsAsync(group, fieldsCategory, values, cancellationToken) : [],
            Categories = BuildCategories(snapshot, categoryId),
            Photos = ad is null ? [] : await photos.ListAsync(ad.Id, cancellationToken),
            PhotoLimit = photoGroup.MaxPhotos,
            Message = message,
            Warning = warning,
            Pendings = pendings ?? [],
            Takedown = ad is null ? TakedownActions.None : TakedownActions.For(new AdActor(currentUser.UserId, currentUser.IsAdministrator), ad)
        };
    }

    private static string FormatCep(string cep)
    {
        string digits = new((cep ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        return CepRules.IsValid(digits) ? CepRules.Format(digits) : cep;
    }

    private static string[] ReadStored(FieldDefinition field, AdAttributes attributes)
    {
        switch (field.Type)
        {
            case FieldType.Text:
                return attributes.GetString(field.Key) is { Length: > 0 } text ? [text] : [];
            case FieldType.Integer:
            case FieldType.Select:
            case FieldType.ModelYear:
            case FieldType.ManufactureYear:
            case FieldType.CatalogItem:
                return attributes.TryGetInt(field.Key, out int number) ? [number.ToString(CultureInfo.InvariantCulture)] : [];
            case FieldType.Decimal:
                return attributes.TryGetDecimal(field.Key, out decimal value)
                    ? [value.ToString(field.DecimalPlaces == 0 ? "0" : "0." + new string('#', field.DecimalPlaces), PtBr)]
                    : [];
            case FieldType.Money:
                return attributes.TryGetLong(field.Key, out long cents) ? [PriceText.Format(cents)] : [];
            case FieldType.MultiSelect:
                return [.. attributes.GetInts(field.Key).Select(i => i.ToString(CultureInfo.InvariantCulture))];
            default:
                return [];
        }
    }

    // Categorias postáveis em <optgroup> por categoria principal; as de terceiro nível levam o caminho ("Autopeças › Pneus")
    private static List<AdCategoryGroup> BuildCategories(CategoryTreeSnapshot snapshot, int? selected)
    {
        List<AdCategoryGroup> groups = [];
        foreach (CategoryNode root in snapshot.Roots)
        {
            List<AdCategoryOption> items = [];
            if (root.IsPostable)
            {
                items.Add(new AdCategoryOption(root.Id, root.Name, root.Id == selected));
            }

            foreach (CategoryNode node in snapshot.DescendantsOf(root.Id).Where(n => n.IsPostable))
            {
                string label = node.Depth == 3 && node.ParentId is { } parent ? $"{snapshot.Find(parent)?.Name} › {node.Name}" : node.Name;
                items.Add(new AdCategoryOption(node.Id, label, node.Id == selected));
            }

            if (items.Count > 0)
            {
                groups.Add(new AdCategoryGroup(root.Name, items));
            }
        }

        return groups;
    }

    private async Task<IReadOnlyList<AdFieldViewModel>> BuildFieldsAsync(FieldGroup group, int categoryId, AdFormSubmission values, CancellationToken cancellationToken)
    {
        int currentYear = ModelYearRules.CurrentYear(time);
        IReadOnlyList<FieldDefinition> definitions = group.FieldsFor(categoryId);
        Dictionary<string, string[]> posted = values.Fields ?? [];
        List<AdFieldViewModel> result = [];

        // Para a cadeia do catálogo: o que foi escolhido em cada nível decide as opções do nível seguinte
        string catalogKind = definitions.FirstOrDefault(f => f.Type == FieldType.CatalogItem)?.Catalog.Kind == CatalogKind.Motorcycle ? VehicleKinds.Moto : VehicleKinds.Car;
        int? brandId = null;
        int? modelId = null;
        int? year = null;

        foreach (FieldDefinition field in definitions)
        {
            string[] raw = posted.TryGetValue(field.Key, out string[] given) ? [.. given.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim())] : [];
            AdFieldViewModel built = field.Type switch
            {
                FieldType.Text => Text(field, raw),
                FieldType.Integer => Integer(field, raw),
                FieldType.Decimal => Number(field, raw, "decimal"),
                FieldType.Money => Number(field, raw, "decimal"),
                FieldType.Select => SelectField(field, raw, field.OptionsFor(categoryId)),
                FieldType.MultiSelect => MultiSelectField(field, raw, field.OptionsFor(categoryId)),
                FieldType.ModelYear => SelectField(field, raw, FieldLists.ModelYears(currentYear)),
                FieldType.ManufactureYear => SelectField(field, raw, FieldLists.ManufactureYears(currentYear)),
                FieldType.CatalogItem => await CatalogFieldAsync(field, raw, catalogKind, Chain(field, ref brandId, ref modelId, ref year, raw), cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(group), field.Type, null)
            };
            built.Required = field.IsRequiredFor(categoryId);
            result.Add(built);
        }

        return result;
    }

    // Guarda a escolha do nível e devolve o que vale como "pai" para as opções dele: (marca, modelo, ano)
    private static (int? Brand, int? Model, int? Year) Chain(FieldDefinition field, ref int? brand, ref int? model, ref int? year, string[] raw)
    {
        (int? Brand, int? Model, int? Year) parents = (brand, model, year);
        if (raw.Length == 1 && int.TryParse(raw[0], NumberStyles.None, CultureInfo.InvariantCulture, out int chosen))
        {
            switch (field.Catalog.Level)
            {
                case CatalogLevel.Brand:
                    brand = chosen;
                    break;
                case CatalogLevel.Model:
                    model = chosen;
                    break;
                case CatalogLevel.Year:
                    year = chosen;
                    break;
            }
        }

        return parents;
    }

    private static AdFieldViewModel Text(FieldDefinition field, string[] raw) => Base(field, AdFieldKind.Text, raw.FirstOrDefault(), raw.FirstOrDefault(), maxLength: field.MaxLength);

    private static AdFieldViewModel Number(FieldDefinition field, string[] raw, string inputMode) =>
        Base(field, AdFieldKind.Number, raw.FirstOrDefault(), raw.FirstOrDefault(), inputMode: inputMode);

    // O inteiro limita o tamanho ao do maior valor permitido: sem teto declarado vale o de int (10 dígitos). O maxlength conta os pontos de milhar que a máscara escreve (9.999.999 = 9).
    private static AdFieldViewModel Integer(FieldDefinition field, string[] raw)
    {
        int digits = ((long)(field.Max ?? int.MaxValue)).ToString(CultureInfo.InvariantCulture).Length;
        return Base(field, AdFieldKind.Number, raw.FirstOrDefault(), raw.FirstOrDefault(), inputMode: "numeric", maxLength: digits + (digits - 1) / 3, maxDigits: digits);
    }

    private static AdFieldViewModel SelectField(FieldDefinition field, string[] raw, FieldList list)
    {
        string chosen = raw.FirstOrDefault();
        List<AdOption> options = [.. (list?.Options ?? []).Select(o => new AdOption(Id(o.Id), o.Label, Id(o.Id) == chosen))];
        AdOption selected = options.FirstOrDefault(o => o.Selected);
        return Base(field, AdFieldKind.Select, selected?.Value, selected?.Label, options);
    }

    private static AdFieldViewModel MultiSelectField(FieldDefinition field, string[] raw, FieldList list)
    {
        HashSet<string> chosen = [.. raw];
        List<AdOption> options = [.. (list?.Options ?? []).Select(o => new AdOption(Id(o.Id), o.Label, chosen.Contains(Id(o.Id))))];
        return Base(field, AdFieldKind.MultiSelect, null, string.Join(", ", options.Where(o => o.Selected).Select(o => o.Label)), options);
    }

    private async Task<AdFieldViewModel> CatalogFieldAsync(
        FieldDefinition field, string[] raw, string kind, (int? Brand, int? Model, int? Year) parents, CancellationToken cancellationToken)
    {
        string chosen = raw.FirstOrDefault();
        IReadOnlyList<(string Value, string Label)> items = field.Catalog.Level switch
        {
            CatalogLevel.Brand => [.. (await SafeAsync(() => catalog.BrandsAsync(kind, cancellationToken))).Select(i => (Id(i.Id), i.Name))],
            CatalogLevel.Model when parents.Brand is { } brand =>
                [.. (await SafeAsync(() => catalog.ModelsAsync(kind, brand, cancellationToken))).Select(i => (Id(i.Id), i.Name))],
            CatalogLevel.Year when parents.Model is { } model =>
                [.. (await SafeAsync(() => catalog.YearsAsync(kind, model, cancellationToken))).Select(y => (Id(y), Id(y)))],
            CatalogLevel.Version when parents is { Model: { } model, Year: { } year } =>
                [.. (await SafeAsync(() => catalog.VersionsAsync(kind, model, year, cancellationToken))).Select(i => (Id(i.Id), i.Name))],
            _ => []
        };

        List<AdOption> options = [.. items.Select(i => new AdOption(i.Value, i.Label, i.Value == chosen))];
        AdOption selected = options.FirstOrDefault(o => o.Selected);
        AdFieldViewModel built = Base(field, AdFieldKind.Select, selected?.Value, selected?.Label, options, disabled: options.Count == 0);
        return built;
    }

    private static async Task<IReadOnlyList<T>> SafeAsync<T>(Func<Task<IReadOnlyList<T>>> query)
    {
        try
        {
            return await query();
        }
        catch (NotFoundException)
        {
            return [];
        }
    }

    private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);

    private static AdFieldViewModel Base(
        FieldDefinition field, AdFieldKind kind, string value, string display, IReadOnlyList<AdOption> options = null, string inputMode = null, int? maxLength = null, bool disabled = false, int? maxDigits = null) => new()
        {
            Key = field.Key,
            Label = field.Label,
            Kind = kind,
            Required = field.Required,
            HelpText = field.HelpText,
            Value = value,
            DisplayValue = display,
            MaxLength = maxLength,
            InputMode = inputMode,
            MaxDigits = maxDigits,
            Options = options ?? [],
            Disabled = disabled,
            CatalogKind = field.Catalog is null ? null : field.Catalog.Kind == CatalogKind.Motorcycle ? VehicleKinds.Moto : VehicleKinds.Car,
            CatalogLevel = field.Catalog?.Level switch
            {
                CatalogLevel.Brand => "brand",
                CatalogLevel.Model => "model",
                CatalogLevel.Year => "year",
                CatalogLevel.Version => "version",
                _ => null
            }
        };
}

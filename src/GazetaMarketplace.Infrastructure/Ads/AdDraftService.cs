using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <inheritdoc cref="IAdDraftService"/>
/// <remarks>
/// O formulário é conferido <b>inteiro</b> antes de qualquer gravação: se algo está errado, nada é salvo e a recusa traz todos os campos de uma vez.
/// A situação, o autor e as datas de publicação nunca vêm do formulário (RC-14). A edição por outra pessoa entre a abertura e o salvamento é
/// detectada pela versão da linha (<c>RowVersion</c>); a auditoria grava só os <i>nomes</i> dos campos alterados (D6), nunca o conteúdo.
/// </remarks>
public sealed class AdDraftService(
    AppDbContext context,
    ICurrentUser currentUser,
    IAdService ads,
    ICategoryTree categories,
    ICepService cep,
    ICityDirectory cities,
    IVehicleCatalog catalog,
    IAuditLog audit,
    TimeProvider time) : IAdDraftService
{
    private const string TargetType = "Ad";

    private sealed record Prepared(
        int? CategoryId,
        string Title,
        string Description,
        long? PriceCents,
        string Cep,
        string City,
        string Uf,
        bool Manual,
        AdAttributes Attributes,
        LocationOutcome Location);

    public async Task<AdDraftResult> CreateAsync(AdDraftInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        int authorId = currentUser.UserId ?? throw new UnauthorizedException();
        Prepared prepared = await PrepareAsync(input, photoCount: 0, cancellationToken);

        try
        {
            IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
            Ad ad = await strategy.ExecuteAsync(async () =>
            {
                context.ChangeTracker.Clear();
                await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                Ad created = Ad.CreateDraft(prepared.Title, authorId);
                Apply(created, prepared);
                context.Ads.Add(created);
                await context.SaveChangesAsync(cancellationToken); // o id só existe depois de gravar

                (string previous, string current) = Describe(null, created, prepared.Attributes);
                await audit.RecordAsync(
                    new AuditRecord("ad.create", TargetType, Id(created.Id), AuditResult.Success, previous, current), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return created;
            });
            return new AdDraftResult(ad.Id, ad.RowVersion, prepared.Location);
        }
        catch (DbUpdateException)
        {
            throw await TranslateAsync(prepared.CategoryId, cancellationToken);
        }
    }

    public async Task<AdDraftResult> UpdateAsync(int id, byte[] rowVersion, AdDraftInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Ad ad = await ads.GetForEditAsync(id, cancellationToken);
        int photoCount = await context.AdPhotos.CountAsync(p => p.AdId == id, cancellationToken);
        Prepared prepared = await PrepareAsync(input, photoCount, cancellationToken);

        // A versão que o formulário trazia: se a linha mudou desde então, a gravação falha com ConflictException
        if (ad.RowVersion is { Length: > 0 })
        {
            if (rowVersion is not { Length: > 0 })
            {
                throw new ConflictException("O anúncio foi alterado por outra pessoa. Recarregue a página e tente de novo.");
            }

            context.Entry(ad).Property(a => a.RowVersion).OriginalValue = rowVersion;
        }

        AdAttributes before = AdAttributes.TryParse(ad.Attributes, out AdAttributes parsed) ? parsed : new AdAttributes();
        Ad snapshot = Snapshot(ad);
        Apply(ad, prepared);
        context.ChangeTracker.DetectChanges();
        if (context.Entry(ad).State == EntityState.Unchanged)
        {
            return new AdDraftResult(ad.Id, ad.RowVersion, prepared.Location); // nada mudou: não grava nem audita
        }

        (string previous, string current) = Describe(snapshot, ad, prepared.Attributes, before);
        try
        {
            await audit.RecordAsync(
                new AuditRecord("ad.update", TargetType, Id(ad.Id), AuditResult.Success, previous, current), cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw await TranslateAsync(prepared.CategoryId, cancellationToken);
        }

        return new AdDraftResult(ad.Id, ad.RowVersion, prepared.Location);
    }

    private static void Apply(Ad ad, Prepared prepared)
    {
        ad.SetText(prepared.Title, prepared.Description);
        ad.SetCategory(prepared.CategoryId);
        ad.SetPrice(prepared.PriceCents);
        ad.SetLocation(prepared.Cep, prepared.City, prepared.Uf, prepared.Manual);
        ad.SetAttributes(prepared.Attributes);
    }

    // Só o que a auditoria precisa para comparar: categoria e preço antes da mudança
    private static Ad Snapshot(Ad ad)
    {
        Ad copy = Ad.CreateDraft(ad.Title, ad.AuthorId);
        copy.SetCategory(ad.CategoryId);
        if (ad.PriceCents is not null)
        {
            copy.SetPrice(ad.PriceCents);
        }

        return copy;
    }

    // D6: categoria e preço (valores), campos (só os nomes das chaves que mudaram). Título e descrição não entram.
    private static (string Previous, string Current) Describe(Ad before, Ad after, AdAttributes now, AdAttributes was = null)
    {
        List<string> previous = [];
        List<string> current = [];
        if (before is null || before.CategoryId != after.CategoryId)
        {
            AddIfSet(previous, "categoria", before?.CategoryId);
            AddIfSet(current, "categoria", after.CategoryId);
        }

        if (before is null || before.PriceCents != after.PriceCents)
        {
            AddIfSet(previous, "preço (centavos)", before?.PriceCents);
            AddIfSet(current, "preço (centavos)", after.PriceCents);
        }

        string[] changedKeys = [.. now.Keys.Concat(was?.Keys ?? []).Distinct(StringComparer.Ordinal)
            .Where(key => was is null ? now.Contains(key) : now.GetRaw(key) != was.GetRaw(key))
            .Order(StringComparer.Ordinal)];
        if (changedKeys.Length > 0)
        {
            current.Add("campos: " + string.Join(", ", changedKeys));
        }

        return (previous.Count == 0 ? null : string.Join("; ", previous), current.Count == 0 ? null : string.Join("; ", current));
    }

    private static void AddIfSet(List<string> parts, string name, long? value)
    {
        if (value is { } v)
        {
            parts.Add(name + ": " + v.ToString(CultureInfo.InvariantCulture));
        }
    }

    private async Task<Prepared> PrepareAsync(AdDraftInput input, int photoCount, CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = [];
        CategoryTreeSnapshot tree = await categories.GetAsync(cancellationToken);

        int? categoryId = null;
        FieldGroup group = FieldGroupRegistry.Default;
        if (input.CategoryId is { } requested)
        {
            CategoryNode node = tree.Find(requested);
            if (node is null || !node.IsPostable)
            {
                errors["CategoryId"] = [AdMessages.CategoryInvalid];
            }
            else
            {
                categoryId = requested;
                group = FieldGroupRegistry.Resolve(tree, requested);
            }
        }

        string title = input.Title?.Trim();
        Check(errors, "Title", AdFormRules.TitleError(group, title));
        string description = AdFormRules.NormalizeLineBreaks(input.Description);
        if (string.IsNullOrWhiteSpace(description))
        {
            description = null;
        }

        Check(errors, "Description", AdFormRules.DescriptionError(group, description));
        Check(errors, "Price", AdFormRules.PriceError(group, input.Price, out long? priceCents));
        if (categoryId is not null)
        {
            Check(errors, "CategoryId", AdFormRules.PhotoLimitError(group, photoCount));
        }

        AdAttributes attributes = new();
        if (categoryId is { } category)
        {
            int currentYear = ModelYearRules.CurrentYear(time);
            foreach (FieldDefinition field in group.FieldsFor(category))
            {
                string[] raw = input.Fields is not null && input.Fields.TryGetValue(field.Key, out string[] values) ? values : null;
                Check(errors, FieldKey(field.Key), FieldValueParser.Apply(field, category, raw, attributes, currentYear));
            }

            await CheckCatalogChainAsync(group, category, attributes, errors, cancellationToken);
        }

        LocationResult location = await ResolveLocationAsync(input, errors, cancellationToken);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new Prepared(categoryId, title, description, priceCents, location.Cep, location.City, location.Uf, location.Manual, attributes, location.Outcome);
    }

    private static string FieldKey(string key) => $"Fields[{key}]";

    private static void Check(Dictionary<string, string[]> errors, string key, string message)
    {
        if (message is not null && !errors.ContainsKey(key))
        {
            errors[key] = [message];
        }
    }

    // D7: o modelo é da marca, o ano é do modelo e a versão é do ano. A cadeia pode estar incompleta no rascunho, mas o que existe não pode contradizer o catálogo.
    private async Task CheckCatalogChainAsync(FieldGroup group, int categoryId, AdAttributes attributes, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        FieldDefinition[] chain = [.. group.FieldsFor(categoryId).Where(f => f.Type == FieldType.CatalogItem)];
        if (chain.Length == 0)
        {
            return;
        }

        string kind = chain[0].Catalog.Kind == CatalogKind.Motorcycle ? VehicleKinds.Moto : VehicleKinds.Car;
        FieldDefinition brandField = chain.Single(f => f.Catalog.Level == CatalogLevel.Brand);
        FieldDefinition modelField = chain.Single(f => f.Catalog.Level == CatalogLevel.Model);
        FieldDefinition yearField = chain.Single(f => f.Catalog.Level == CatalogLevel.Year);
        FieldDefinition versionField = chain.Single(f => f.Catalog.Level == CatalogLevel.Version);

        bool hasBrand = attributes.TryGetInt(brandField.Key, out int brandId);
        bool hasModel = attributes.TryGetInt(modelField.Key, out int modelId);
        bool hasYear = attributes.TryGetInt(yearField.Key, out int year);
        bool hasVersion = attributes.TryGetInt(versionField.Key, out int versionId);

        bool brandOk = hasBrand && !errors.ContainsKey(FieldKey(brandField.Key));
        if (hasBrand && brandOk)
        {
            IReadOnlyList<CatalogItem> brands = await SafeAsync(() => catalog.BrandsAsync(kind, cancellationToken));
            brandOk = brands.Any(b => b.Id == brandId);
            if (!brandOk)
            {
                Check(errors, FieldKey(brandField.Key), "Escolha uma marca da lista");
            }
        }

        bool modelOk = false;
        if (hasModel && !errors.ContainsKey(FieldKey(modelField.Key)))
        {
            if (!brandOk)
            {
                Check(errors, FieldKey(modelField.Key), hasBrand ? "Escolha uma marca válida antes do modelo" : "Escolha a marca antes do modelo");
            }
            else
            {
                IReadOnlyList<CatalogItem> models = await SafeAsync(() => catalog.ModelsAsync(kind, brandId, cancellationToken));
                modelOk = models.Any(m => m.Id == modelId);
                if (!modelOk)
                {
                    Check(errors, FieldKey(modelField.Key), "Este modelo não pertence à marca escolhida");
                }
            }
        }

        bool yearOk = false;
        if (hasYear && !errors.ContainsKey(FieldKey(yearField.Key)))
        {
            if (!modelOk)
            {
                Check(errors, FieldKey(yearField.Key), "Escolha o modelo antes do ano");
            }
            else
            {
                IReadOnlyList<int> years = await SafeAsync(() => catalog.YearsAsync(kind, modelId, cancellationToken));
                yearOk = years.Contains(year);
                if (!yearOk)
                {
                    Check(errors, FieldKey(yearField.Key), "Este ano não existe para o modelo escolhido");
                }
            }
        }

        if (hasVersion && !errors.ContainsKey(FieldKey(versionField.Key)))
        {
            if (!yearOk)
            {
                Check(errors, FieldKey(versionField.Key), "Escolha o ano antes da versão");
            }
            else
            {
                IReadOnlyList<CatalogItem> versions = await SafeAsync(() => catalog.VersionsAsync(kind, modelId, year, cancellationToken));
                if (versions.All(v => v.Id != versionId))
                {
                    Check(errors, FieldKey(versionField.Key), "Esta versão não pertence ao ano escolhido");
                }
            }
        }
    }

    // Um item que não existe no catálogo é "lista vazia" para quem confere (a recusa vem do campo que o citou)
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

    private sealed record LocationResult(string Cep, string City, string Uf, bool Manual, LocationOutcome Outcome);

    // D4: o servidor nunca confia na cidade/UF do navegador sem conferir. UF precisa existir; a cidade precisa estar na lista da UF (ou, se a UF
    // não tem lista carregada, é só padronizada). Sem cidade/UF do navegador, o servidor consulta o CEP.
    private async Task<LocationResult> ResolveLocationAsync(AdDraftInput input, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        string cepText = (input.Cep ?? string.Empty).Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).Replace(".", string.Empty, StringComparison.Ordinal);
        string city = input.City?.Trim() ?? string.Empty;
        string uf = input.Uf?.Trim() ?? string.Empty;
        bool hasCep = cepText.Length > 0;
        if (hasCep && !CepRules.IsValid(cepText))
        {
            Check(errors, "Cep", CepRules.IncompleteMessage);
            return new LocationResult(null, null, null, false, LocationOutcome.None);
        }

        bool browserLocation = city.Length > 0 && uf.Length > 0 && (input.LocationManual || input.LocationCep == cepText);
        if (input.LocationManual)
        {
            return await ValidateInformedAsync(hasCep ? cepText : null, city, uf, manual: true, errors, cancellationToken);
        }

        if (!hasCep)
        {
            if (city.Length > 0 || uf.Length > 0)
            {
                Check(errors, "Cep", AdMessages.LocationOnlyWithCep);
            }

            return new LocationResult(null, null, null, false, LocationOutcome.None);
        }

        if (browserLocation)
        {
            return await ValidateInformedAsync(cepText, city, uf, manual: false, errors, cancellationToken);
        }

        try
        {
            CepResult found = await cep.GetAsync(cepText, cancellationToken);
            return new LocationResult(cepText, found.City, found.Uf, false, LocationOutcome.Resolved);
        }
        catch (NotFoundException)
        {
            return new LocationResult(cepText, null, null, false, LocationOutcome.CepNotFound);
        }
        catch (ServiceUnavailableException)
        {
            return new LocationResult(cepText, null, null, false, LocationOutcome.CepUnavailable);
        }
    }

    private async Task<LocationResult> ValidateInformedAsync(string cepText, string city, string uf, bool manual, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (city.Length == 0 && uf.Length == 0)
        {
            // Modo manual aberto e ainda sem escolha: o rascunho guarda só o CEP
            return new LocationResult(cepText, null, null, false, LocationOutcome.None);
        }

        State state = BrazilianStates.Find(uf);
        if (state is null)
        {
            Check(errors, "Uf", AdMessages.UfInvalid);
            return new LocationResult(cepText, null, null, false, LocationOutcome.None);
        }

        if (city.Length == 0)
        {
            Check(errors, "City", AdMessages.CityRequiredWithUf);
            return new LocationResult(cepText, null, null, false, LocationOutcome.None);
        }

        IReadOnlyList<CityItem> list = await cities.ByUfAsync(state.Uf, cancellationToken);
        string standardized;
        if (list.Count > 0)
        {
            string wanted = Normalizer.Normalize(city);
            CityItem match = list.FirstOrDefault(c => Normalizer.Normalize(c.Name) == wanted);
            if (match is null)
            {
                Check(errors, "City", AdMessages.CityNotInUf);
                return new LocationResult(cepText, null, null, false, LocationOutcome.None);
            }

            standardized = match.Name;
        }
        else
        {
            standardized = CityNames.Standardize(city);
            if (standardized.Length > Ad.CityMaxLength)
            {
                Check(errors, "City", AdMessages.CityNotInUf);
                return new LocationResult(cepText, null, null, false, LocationOutcome.None);
            }
        }

        return new LocationResult(cepText, standardized, state.Uf, manual, LocationOutcome.Informed);
    }

    // A categoria pode ter sido apagada entre a conferência e a gravação: o banco recusou pela chave estrangeira
    private async Task<Exception> TranslateAsync(int? categoryId, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        if (categoryId is { } id && !await context.Categories.AnyAsync(c => c.Id == id, cancellationToken))
        {
            return new ValidationException(new Dictionary<string, string[]> { ["CategoryId"] = [AdMessages.CategoryGone] });
        }

        return new ConflictException("Não foi possível salvar o anúncio agora. Tente de novo.");
    }

    private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);
}

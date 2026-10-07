using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Location;

/// <inheritdoc cref="ICepService"/>
/// <remarks>
/// Entrada do cache vale 30 dias; vencida, é ignorada e o serviço externo é consultado (se ele falhar, 503: o dado vencido não é servido, ADR-007).
/// Só CEP encontrado entra no cache (um CEP inexistente pode passar a existir). O nome da cidade vem da lista do IBGE pelo código; sem lista, vale a
/// padronização da SPEC. O cache guarda o nome já padronizado.
/// </remarks>
public sealed class CepService(AppDbContext context, ICepLookup lookup, ICityDirectory cities, TimeProvider time) : ICepService
{
    private static readonly TimeSpan Validity = TimeSpan.FromDays(ICepService.CacheDays);

    public async Task<CepResult> GetAsync(string cep, CancellationToken cancellationToken)
    {
        if (!CepRules.IsValid(cep))
        {
            throw new ValidationException(new System.Collections.Generic.Dictionary<string, string[]> { ["cep"] = [CepRules.IncompleteMessage] });
        }

        DateTime now = time.GetUtcNow().UtcDateTime;
        CepCacheEntry cached = await context.CepCache.AsNoTracking().SingleOrDefaultAsync(e => e.Cep == cep, cancellationToken);
        if (cached is not null && now - cached.FetchedAt < Validity)
        {
            return new CepResult(cep, cached.City, cached.Uf, CepResult.FromCache);
        }

        CepLookupResult found = await lookup.LookupAsync(cep, cancellationToken)
            ?? throw new NotFoundException(CepRules.NotFoundMessage);

        (string city, string uf) = await StandardizeAsync(found, cancellationToken);
        await StoreAsync(cep, city, uf, found.IbgeCode, now, cancellationToken);
        return new CepResult(cep, city, uf, CepResult.FromViaCep);
    }

    private async Task<(string City, string Uf)> StandardizeAsync(CepLookupResult found, CancellationToken cancellationToken)
    {
        if (found.IbgeCode is { } code && await cities.FindByCodeAsync(code, cancellationToken) is { } official)
        {
            return (official.Name, official.Uf);
        }

        return (CityNames.Standardize(found.City), found.Uf);
    }

    private async Task StoreAsync(string cep, string city, string uf, int? ibgeCode, DateTime now, CancellationToken cancellationToken)
    {
        CepCacheEntry entry = await context.CepCache.SingleOrDefaultAsync(e => e.Cep == cep, cancellationToken);
        if (entry is null)
        {
            entry = new CepCacheEntry { Cep = cep, City = city, Uf = uf, IbgeCode = ibgeCode, FetchedAt = now };
            context.CepCache.Add(entry);
        }
        else
        {
            entry.City = city;
            entry.Uf = uf;
            entry.IbgeCode = ibgeCode;
            entry.FetchedAt = now;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Outra pessoa gravou o mesmo CEP ao mesmo tempo: o cache já tem o dado, e a resposta desta chamada continua valendo.
            // Solta só a entrada do cache: o contexto é o da requisição inteira e pode estar rastreando o anúncio que a pessoa está editando (R-03)
            context.Entry(entry).State = EntityState.Detached;
        }
    }
}

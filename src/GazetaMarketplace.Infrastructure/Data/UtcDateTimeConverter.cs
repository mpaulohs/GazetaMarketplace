using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>O banco guarda datetime2 sem fuso; ao ler, marca como UTC.</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

internal sealed class UtcNullableDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    value => value,
    value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

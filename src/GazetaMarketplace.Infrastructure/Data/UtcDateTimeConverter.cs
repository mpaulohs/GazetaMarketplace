using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>O banco guarda datetime2 sem fuso; ao ler, marca como UTC.</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    valor => valor,
    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

internal sealed class UtcNullableDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    valor => valor,
    valor => valor.HasValue ? DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc) : valor);

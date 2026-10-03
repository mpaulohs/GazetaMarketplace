using System;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>
/// Só para o <c>dotnet ef</c> (migrations e script). A cadeia é fictícia e nunca é aberta: gerar
/// migration ou script não conecta ao banco. Valores reais vêm de variáveis de ambiente (ADR-011).
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(local);Database=GazetaMarketplace;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new AppDbContext(options, new SystemUser(), TimeProvider.System);
    }
}

/// <summary>Contexto sem requisição: as ações são do sistema (autor nulo).</summary>
internal sealed class SystemUser : ICurrentUser
{
    public int? UserId => null;

    public string CorrelationId => null;
}

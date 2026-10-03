using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>
/// Acesso Dapper (ADR-004): leitura complexa e escrita justificada. Regras de uso:
/// toda escrita (<c>Execute*</c>) leva o comentário <c>// Dapper: motivo</c>; escrita em tabela com
/// <c>RowVersion</c> confere a versão no <c>WHERE</c> e preenche as colunas de auditoria; escrita que precisa
/// ser atômica com o EF usa <see cref="GetConnectionAndTransaction"/>.
/// </summary>
public static class DapperConfiguration
{
    /// <summary><see cref="IDbConnection"/> scoped, com a mesma cadeia do EF Core (lida só quando usada).</summary>
    public static IServiceCollection AddDapper(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDbConnection>(_ => new SqlConnection(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }

    /// <summary>Conexão e transação atuais do <see cref="DbContext"/> (a transação é nula se não houver uma aberta).</summary>
    public static (DbConnection Connection, DbTransaction Transacao) GetConnectionAndTransaction(this DbContext context)
    {
        IDbContextTransaction current = context.Database.CurrentTransaction;
        return (context.Database.GetDbConnection(), current?.GetDbTransaction());
    }
}

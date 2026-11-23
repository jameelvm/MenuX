using MenuX.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MenuX.Infrastructure.Persistence;

public static class PostgresDbContextOptions
{
    /// <summary>
    /// Configures a module DbContext for PostgreSQL: snake_case names and the tenant-session interceptor
    /// that applies <c>SET LOCAL app.tenant_id</c> to every transaction (Row-Level Security), and a guard
    /// that rejects commands outside a transaction.
    /// </summary>
    public static DbContextOptionsBuilder UseMenuXPostgres(
        this DbContextOptionsBuilder builder, string connectionString, ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new TenantSessionInterceptor(tenantContext), new TransactionRequiredInterceptor());
    }
}

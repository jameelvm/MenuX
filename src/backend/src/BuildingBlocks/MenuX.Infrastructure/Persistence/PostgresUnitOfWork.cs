using MenuX.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MenuX.Infrastructure.Persistence;

/// <summary>
/// One PostgreSQL connection and transaction per request (or job), shared by every module DbContext.
/// Modules keep their own schemas and contexts; sharing the transaction makes a cross-module change
/// (e.g. tenant + owner membership + audit event) atomic in the single application database.
/// The tenant session settings are applied when the transaction begins (<see cref="TenantSession"/>),
/// so the tenant context must be set before the first database access in the request.
/// </summary>
public sealed class PostgresUnitOfWork(NpgsqlDataSource dataSource, ITenantContext tenantContext) : IAsyncDisposable, IDisposable
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;
    private bool _completed;

    public bool HasStarted => _transaction is not null;

    /// <summary>Starts the transaction on first use (synchronous: DbContexts are created by DI factories).</summary>
    public (NpgsqlConnection Connection, NpgsqlTransaction Transaction) GetOrStart()
    {
        ObjectDisposedException.ThrowIf(_completed, this);
        if (_transaction is null)
        {
            _connection = dataSource.OpenConnection();
            _transaction = _connection.BeginTransaction();
            using var command = new NpgsqlCommand(TenantSession.SetConfigSql, _connection, _transaction);
            TenantSession.AddParameters(command, TenantSession.From(tenantContext));
            command.ExecuteNonQuery();
        }

        return (_connection!, _transaction);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null || _completed)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        _completed = true;
    }

    /// <summary>Synchronous variant for scopes disposed synchronously: same rollback-then-release behaviour.</summary>
    public void Dispose()
    {
        if (_transaction is not null)
        {
            if (!_completed)
            {
                _transaction.Rollback();
            }

            _transaction.Dispose();
        }

        _connection?.Dispose();
        _completed = true;
    }

    /// <summary>Rolls back anything not committed, then releases the connection to the pool.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            if (!_completed)
            {
                await _transaction.RollbackAsync().ConfigureAwait(false);
            }

            await _transaction.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _completed = true;
    }
}

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Registers the app-role data source, the scoped tenant context and the unit of work.</summary>
    public static IServiceCollection AddMenuXPersistence(this IServiceCollection services, Func<IServiceProvider, string> appConnectionString)
    {
        ArgumentNullException.ThrowIfNull(appConnectionString);
        // Resolved lazily so configuration overrides (tests, AWS secrets) always apply.
        services.AddSingleton(sp => NpgsqlDataSource.Create(appConnectionString(sp)));
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextSetter>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<PostgresUnitOfWork>();
        return services;
    }

    /// <summary>
    /// Registers a module DbContext on the request's shared connection and transaction, with snake_case
    /// names and the transaction-required guard.
    /// </summary>
    public static IServiceCollection AddTenantScopedDbContext<TContext>(
        this IServiceCollection services, Func<DbContextOptions<TContext>, ITenantContext, TContext> create)
        where TContext : TenantScopedDbContext
    {
        services.AddScoped(sp =>
        {
            var (connection, transaction) = sp.GetRequiredService<PostgresUnitOfWork>().GetOrStart();
            var options = new DbContextOptionsBuilder<TContext>()
                .UseNpgsql(connection)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(new TransactionRequiredInterceptor())
                .Options;
            var context = create(options, sp.GetRequiredService<ITenantContext>());
            context.Database.UseTransaction(transaction);
            return context;
        });
        return services;
    }
}

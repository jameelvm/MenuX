using System.Data.Common;
using MenuX.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MenuX.Infrastructure.Persistence;

/// <summary>
/// Applies the tenant context to every PostgreSQL transaction that EF Core starts itself (doc 09 §4.2),
/// via <see cref="TenantSession"/> (SET LOCAL semantics). Request work normally uses the shared
/// <see cref="PostgresUnitOfWork"/>, which applies the same settings when it begins its transaction.
/// </summary>
public sealed class TenantSessionInterceptor(ITenantContext tenantContext) : DbTransactionInterceptor
{
    public override DbTransaction TransactionStarted(
        DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
    {
        ArgumentNullException.ThrowIfNull(connection);
        using var command = CreateCommand(connection, result);
        command.ExecuteNonQuery();
        return result;
    }

    public override async ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection, TransactionEndEventData eventData, DbTransaction result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        await using var command = CreateCommand(connection, result);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private DbCommand CreateCommand(DbConnection connection, DbTransaction transaction)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
#pragma warning disable CA2100 // Constant SQL; values are parameters.
        command.CommandText = TenantSession.SetConfigSql;
#pragma warning restore CA2100
        TenantSession.AddParameters(command, TenantSession.From(tenantContext));
        return command;
    }
}

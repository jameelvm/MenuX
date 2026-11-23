using System.Data.Common;
using MenuX.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MenuX.Infrastructure.Persistence;

/// <summary>
/// Applies the tenant context to every PostgreSQL transaction (doc 09 §4.2):
/// <c>set_config('app.tenant_id', …, is_local => true)</c>, which is <c>SET LOCAL</c>. The setting ends with
/// the transaction, so a pooled connection never carries a previous tenant. Work outside a transaction
/// sees no tenant, and Row-Level Security then returns no rows: the failure mode is safe.
/// </summary>
public sealed class TenantSessionInterceptor(ITenantContext tenantContext) : DbTransactionInterceptor
{
    internal const string Sql =
        "SELECT set_config('app.tenant_id', @tenant_id, true), set_config('app.user_id', @user_id, true)";

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
        command.CommandText = Sql;
#pragma warning restore CA2100
        AddParameter(command, "tenant_id", tenantContext.TenantId);
        AddParameter(command, "user_id", tenantContext.UserId);
        return command;
    }

    private static void AddParameter(DbCommand command, string name, Guid? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        // Empty string = not set; the RLS policies treat it as NULL (nullif), so no rows match.
        parameter.Value = value?.ToString() ?? string.Empty;
        command.Parameters.Add(parameter);
    }
}

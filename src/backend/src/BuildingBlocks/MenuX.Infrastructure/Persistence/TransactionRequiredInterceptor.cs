using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MenuX.Infrastructure.Persistence;

/// <summary>
/// Fails fast when a tenant-scoped command runs outside a transaction. Row-Level Security relies on
/// <c>SET LOCAL app.tenant_id</c>, which only exists inside a transaction; without one, queries would
/// silently return no rows. <c>SaveChanges</c> opens its own transaction; reads use
/// <see cref="TenantTransactions.InTenantTransactionAsync{T}"/>.
/// </summary>
public sealed class TransactionRequiredInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Guard(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Guard(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Guard(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Guard(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Guard(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Guard(command);
        return ValueTask.FromResult(result);
    }

    private static void Guard(DbCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Transaction is null)
        {
            throw new InvalidOperationException(
                "Tenant-scoped database commands must run inside a transaction so Row-Level Security sees " +
                "SET LOCAL app.tenant_id. Use InTenantTransactionAsync for reads.");
        }
    }
}

public static class TenantTransactions
{
    /// <summary>Runs <paramref name="work"/> in a transaction, so the tenant session settings apply.</summary>
    public static async Task<T> InTenantTransactionAsync<T>(
        this DbContext context, Func<Task<T>> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(work);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var result = await work().ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}

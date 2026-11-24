using MenuX.Infrastructure.Persistence;
using MenuX.Infrastructure.Tenancy;
using MenuX.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MenuX.Tenancy.Infrastructure;

internal sealed class TenancyApi(NpgsqlDataSource dataSource, IServiceProvider services) : ITenancyApi
{
    public async Task<TenantState?> GetTenantStateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = await TenantSession.BeginAsync(
            dataSource, new TenantContextValues(tenantId, null), cancellationToken).ConfigureAwait(false);
        await using (connection)
        await using (transaction)
        {
            await using var command = new NpgsqlCommand(
                "SELECT status, plan_key, subscription_status, trial_ends_at FROM tenancy.tenants WHERE id = @t",
                connection, transaction);
            command.Parameters.AddWithValue("t", tenantId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return new TenantState(
                tenantId, reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3));
        }
    }

    public async Task<bool> LocationsBelongToTenantAsync(IReadOnlyCollection<Guid> locationIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locationIds);
        if (locationIds.Count == 0)
        {
            return true;
        }

        var db = (TenancyDbContext)services.GetService(typeof(TenancyDbContext))!;
        var distinct = locationIds.Distinct().ToList();
        var found = await db.Locations.CountAsync(l => distinct.Contains(l.Id), cancellationToken).ConfigureAwait(false);
        return found == distinct.Count;
    }
}

/// <summary>
/// Placeholder billing policy: allows every operation. "Restaurant usage requires an active subscription
/// or an eligible free trial" (product owner, 2026-11-23), but no trial duration, pricing or access rules
/// are defined yet (billing phase, U7). Replace this registration when the rules are decided.
/// </summary>
internal sealed class BillingRulesPendingAccessPolicy : ITenantAccessPolicy
{
    public TenantAccessDecision Evaluate(TenantState state, TenantOperation operation) => TenantAccessDecision.Allow;
}

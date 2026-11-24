namespace MenuX.Tenancy.Contracts;

/// <summary>
/// A tenant's lifecycle and commercial state. Commercial fields mirror the future billing records
/// (U7: billing deferred; plan_key + static limits). ASSUMPTION: no trial duration, pricing or access
/// rules are defined yet; they are finalised in the billing phase.
/// </summary>
public sealed record TenantState(
    Guid TenantId, string Status, string PlanKey, string SubscriptionStatus, DateTimeOffset? TrialEndsAt);

/// <summary>Tenancy & Org public API (doc 05 §3).</summary>
public interface ITenancyApi
{
    /// <summary>The tenant's state, or null if it doesn't exist (read outside the request unit of work).</summary>
    Task<TenantState?> GetTenantStateAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Within the current unit of work: true if every location belongs to the selected tenant.</summary>
    Task<bool> LocationsBelongToTenantAsync(IReadOnlyCollection<Guid> locationIds, CancellationToken cancellationToken = default);
}

public enum TenantOperation
{
    Read,
    Write,
}

public sealed record TenantAccessDecision(bool Allowed, string? Reason)
{
    public static TenantAccessDecision Allow { get; } = new(true, null);
}

/// <summary>
/// Billing seam: decides whether a tenant may perform an operation given its commercial state
/// ("restaurant usage requires an active subscription or an eligible free trial", product owner,
/// 2026-11-23). The rules are NOT defined yet (billing phase); the default implementation allows
/// everything and only exists so the rules can be added in one place later.
/// </summary>
public interface ITenantAccessPolicy
{
    TenantAccessDecision Evaluate(TenantState state, TenantOperation operation);
}

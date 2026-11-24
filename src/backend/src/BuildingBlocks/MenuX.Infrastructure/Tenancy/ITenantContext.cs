namespace MenuX.Infrastructure.Tenancy;

/// <summary>
/// The tenant, user and effective permissions for the current unit of work (doc 09 §2). Resolved once
/// per request (or per job message) from the verified membership, never from request payloads, and
/// read-only for handlers and repositories.
/// </summary>
public interface ITenantContext
{
    /// <summary>The selected tenant, or <c>null</c> when no tenant has been resolved.</summary>
    Guid? TenantId { get; }

    /// <summary>The authenticated MenuX user, or <c>null</c> for anonymous or system work.</summary>
    Guid? UserId { get; }

    /// <summary>Permission keys granted by the user's roles in the selected tenant (doc 03 §2).</summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>Locations the membership is limited to; <c>null</c> = all locations of the tenant.</summary>
    IReadOnlyCollection<Guid>? LocationScope { get; }

    /// <summary>Tenants the user belongs to; only set for the tenant picker (read-only access to those tenants).</summary>
    IReadOnlyCollection<Guid> MemberTenantIds { get; }
}

/// <summary>Values applied to the tenant context.</summary>
public sealed record TenantContextValues(
    Guid? TenantId,
    Guid? UserId,
    IReadOnlySet<string>? Permissions = null,
    IReadOnlyCollection<Guid>? LocationScope = null,
    IReadOnlyCollection<Guid>? MemberTenantIds = null);

/// <summary>
/// Sets the tenant context. Only the tenant-resolution middleware, tenant onboarding and job consumers
/// use this (enforced by an architecture test); handlers depend on <see cref="ITenantContext"/>.
/// </summary>
public interface ITenantContextSetter
{
    void Apply(TenantContextValues values);
}

/// <summary>Scoped, per-request holder for <see cref="ITenantContext"/>.</summary>
public sealed class TenantContext : ITenantContext, ITenantContextSetter
{
    private static readonly IReadOnlySet<string> NoPermissions = new HashSet<string>();
    private bool _isSet;

    public Guid? TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public IReadOnlySet<string> Permissions { get; private set; } = NoPermissions;

    public IReadOnlyCollection<Guid>? LocationScope { get; private set; }

    public IReadOnlyCollection<Guid> MemberTenantIds { get; private set; } = [];

    public void Apply(TenantContextValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (_isSet)
        {
            throw new InvalidOperationException("The tenant context can be set only once per scope.");
        }

        TenantId = values.TenantId;
        UserId = values.UserId;
        Permissions = values.Permissions ?? NoPermissions;
        LocationScope = values.LocationScope;
        MemberTenantIds = values.MemberTenantIds ?? [];
        _isSet = true;
    }

    /// <summary>Convenience overload: tenant and user only.</summary>
    public void Apply(Guid? tenantId, Guid? userId) => Apply(new TenantContextValues(tenantId, userId));
}

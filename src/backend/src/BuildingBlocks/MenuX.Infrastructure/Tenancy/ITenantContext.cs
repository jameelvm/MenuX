namespace MenuX.Infrastructure.Tenancy;

/// <summary>
/// The tenant and user for the current unit of work (doc 09 §2). It is resolved once per request
/// (or per job message) from the verified membership, never from request payloads, and is read-only
/// for handlers and repositories.
/// </summary>
public interface ITenantContext
{
    /// <summary>The selected tenant, or <c>null</c> when no tenant has been resolved.</summary>
    Guid? TenantId { get; }

    /// <summary>The authenticated MenuX user, or <c>null</c> for anonymous or system work.</summary>
    Guid? UserId { get; }
}

/// <summary>
/// Sets the tenant context. Only the tenant-resolution middleware and job consumers use this;
/// handlers depend on <see cref="ITenantContext"/>, which has no setter.
/// </summary>
public interface ITenantContextSetter
{
    void Apply(Guid? tenantId, Guid? userId);
}

/// <summary>Scoped, per-request holder for <see cref="ITenantContext"/>.</summary>
public sealed class TenantContext : ITenantContext, ITenantContextSetter
{
    private bool _isSet;

    public Guid? TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public void Apply(Guid? tenantId, Guid? userId)
    {
        if (_isSet)
        {
            throw new InvalidOperationException("The tenant context can be set only once per scope.");
        }

        TenantId = tenantId;
        UserId = userId;
        _isSet = true;
    }
}

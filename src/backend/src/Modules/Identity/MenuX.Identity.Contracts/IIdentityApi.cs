namespace MenuX.Identity.Contracts;

/// <summary>Profile data read from the ID token once, at first login (product owner, 2026-11-23).</summary>
public sealed record FirstLoginProfile(string Issuer, string Subject, string? Email, bool EmailVerified, string? Name);

public enum ProvisioningOutcome
{
    /// <summary>The user already existed; the database stays the source of truth (no profile update).</summary>
    Existing,

    /// <summary>A new MenuX user was created from the verified first-login profile.</summary>
    Created,

    /// <summary>Rejected: the identity provider did not confirm a verified email (API-side enforcement).</summary>
    EmailNotVerified,

    /// <summary>Rejected: the user exists but is disabled.</summary>
    Disabled,

    /// <summary>Rejected: the email belongs to another identity (another issuer/subject).</summary>
    EmailInUse,
}

public sealed record ProvisioningResult(ProvisioningOutcome Outcome, Guid? UserId);

/// <summary>A verified membership with its effective permissions (doc 03 §2) and location scope.</summary>
public sealed record ResolvedMembership(
    Guid TenantId, Guid MembershipId, IReadOnlySet<string> Permissions, IReadOnlyCollection<Guid>? LocationScope);

/// <summary>Identity & Access public API (doc 05 §3). Other modules use only this contract.</summary>
public interface IIdentityApi
{
    /// <summary>At login: finds the user, or creates them from a verified first-login profile.</summary>
    Task<ProvisioningResult> EnsureUserAsync(FirstLoginProfile profile, CancellationToken cancellationToken = default);

    /// <summary>Active user for (issuer, subject), or null.</summary>
    Task<Guid?> FindUserIdAsync(string issuer, string subject, CancellationToken cancellationToken = default);

    /// <summary>The user's active membership in the tenant, or null (no membership, disabled, or unknown tenant).</summary>
    Task<ResolvedMembership?> ResolveMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Tenants where the user has an active membership (tenant picker).</summary>
    Task<IReadOnlyList<Guid>> GetMemberTenantIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>True if the user's email is verified (API-side check before tenant creation).</summary>
    Task<bool> HasVerifiedEmailAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Within the current unit of work: makes the user the Owner of the (new) tenant.</summary>
    Task AddOwnerMembershipAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Permission keys seeded in identity migrations (doc 03 §2).</summary>
public static class Permissions
{
    public const string TenantManage = "tenant.manage";
    public const string TenantDelete = "tenant.delete";
    public const string MemberManage = "member.manage";
    public const string LocationManage = "location.manage";
    public const string AuditRead = "audit.read";
    public const string CatalogEdit = "catalog.edit";
    public const string MenuEdit = "menu.edit";
    public const string MenuPublish = "menu.publish";
    public const string ItemSoldOut = "item.sold_out";
    public const string QrManage = "qr.manage";
    public const string TenantBilling = "tenant.billing";
}

public static class Roles
{
    public const string Owner = "owner";
}

using System.Security.Claims;
using MenuX.Host.Api.Bff;
using MenuX.Identity.Contracts;
using MenuX.Infrastructure.Tenancy;
using MenuX.Tenancy.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace MenuX.Host.Api.Tenancy;

/// <summary>
/// Resolves the tenant context for /api requests (doc 09 §2): the user from the session's iss + sub,
/// the selected tenant from the X-Tenant-Id header, verified against the user's active membership in
/// the database (cached 60 s), with permissions and location scope. Unknown tenants and tenants the user
/// doesn't belong to both answer 404, so existence isn't revealed (doc 09 §4.1).
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string TenantHeader = "X-Tenant-Id";
    internal const string TenantStateItem = "MenuX.TenantState";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public async Task InvokeAsync(
        HttpContext context, IIdentityApi identity, ITenancyApi tenancy, ITenantContextSetter setter, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.Request.Path.StartsWithSegments("/api") || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var ct = context.RequestAborted;
        var issuer = context.User.FindFirstValue(BffAuthentication.IssuerClaim);
        var subject = context.User.FindFirstValue(BffAuthentication.SubjectClaim);
        if (issuer is null || subject is null)
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Session without issuer/subject.");
            return;
        }

        var userId = await cache.GetOrCreateAsync(("user", issuer, subject), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return identity.FindUserIdAsync(issuer, subject, ct);
        });
        if (userId is null)
        {
            // Disabled, or never provisioned (e.g. login rejected for an unverified email).
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "No active MenuX account for this sign-in.");
            return;
        }

        var header = context.Request.Headers[TenantHeader].ToString();
        if (string.IsNullOrEmpty(header))
        {
            // No tenant selected: the user's own data and (read-only) the tenants they belong to.
            // Not cached: cheap, and a newly created tenant must appear in the picker immediately.
            var memberTenants = await identity.GetMemberTenantIdsAsync(userId.Value, ct);
            setter.Apply(new TenantContextValues(null, userId, MemberTenantIds: memberTenants));
            await next(context);
            return;
        }

        if (!Guid.TryParse(header, out var tenantId))
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, $"{TenantHeader} must be a tenant ID.");
            return;
        }

        var membership = await cache.GetOrCreateAsync(("membership", userId.Value, tenantId), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return identity.ResolveMembershipAsync(userId.Value, tenantId, ct);
        });
        var state = membership is null ? null : await cache.GetOrCreateAsync(("tenant-state", tenantId), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return tenancy.GetTenantStateAsync(tenantId, ct);
        });
        if (membership is null || state is null || state.Status == "deleted")
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Tenant not found.");
            return;
        }

        setter.Apply(new TenantContextValues(tenantId, userId, membership.Permissions, membership.LocationScope));
        context.Items[TenantStateItem] = state;
        await next(context);
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string title) =>
        Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
}

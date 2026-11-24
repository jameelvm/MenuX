using MenuX.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Infrastructure.Web;

/// <summary>
/// Authorization by permission, never by role name (doc 03 §2; doc 09 §4 layer L2). The permissions come
/// from the verified membership resolved by the tenant-resolution middleware.
/// </summary>
public static class TenantEndpointFilters
{
    /// <summary>The endpoint needs a selected tenant (X-Tenant-Id header with a verified membership).</summary>
    public static TBuilder RequireTenant<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
            TenantOf(context.HttpContext).TenantId is null
                ? Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Select a tenant (X-Tenant-Id header).")
                : await next(context).ConfigureAwait(false));

    /// <summary>The endpoint needs a selected tenant and the given permission in it.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var tenant = TenantOf(context.HttpContext);
            if (tenant.TenantId is null)
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Select a tenant (X-Tenant-Id header).");
            }

            return tenant.Permissions.Contains(permission)
                ? await next(context).ConfigureAwait(false)
                : Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: $"Missing permission '{permission}'.");
        });

    /// <summary>True if the membership may act on the location (no scope = all locations).</summary>
    public static bool CanAccessLocation(this ITenantContext tenant, Guid locationId)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        return tenant.LocationScope is null || tenant.LocationScope.Contains(locationId);
    }

    private static ITenantContext TenantOf(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<ITenantContext>();
}

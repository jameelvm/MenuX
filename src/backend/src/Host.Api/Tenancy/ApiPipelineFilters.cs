using MenuX.Host.Api.Bff;
using MenuX.Infrastructure.Persistence;
using MenuX.Tenancy.Contracts;

namespace MenuX.Host.Api.Tenancy;

/// <summary>Cross-cutting filters for the /api group.</summary>
public static class ApiPipelineFilters
{
    private static readonly HashSet<string> ReadOnlyStatuses = new(StringComparer.Ordinal) { "suspended", "pending_deletion", "deleting" };

    /// <summary>
    /// State-changing requests: anti-forgery token (doc 09 §5), then the tenant-status gate (suspended or
    /// deleting tenants are read-only; doc 07 §7), then the billing access policy (rules pending, U7).
    /// </summary>
    public static async ValueTask<object?> GuardWrites(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var http = context.HttpContext;
        if (HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method) || HttpMethods.IsOptions(http.Request.Method))
        {
            return await next(context);
        }

        var antiforgery = await AntiforgeryValidation.RequireAntiforgeryToken(context, _ => ValueTask.FromResult<object?>(null));
        if (antiforgery is not null)
        {
            return antiforgery;
        }

        if (http.Items[TenantResolutionMiddleware.TenantStateItem] is TenantState state)
        {
            if (ReadOnlyStatuses.Contains(state.Status))
            {
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: $"This business is {state.Status.Replace('_', ' ')}; changes are disabled.");
            }

            var decision = http.RequestServices.GetRequiredService<ITenantAccessPolicy>().Evaluate(state, TenantOperation.Write);
            if (!decision.Allowed)
            {
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: decision.Reason ?? "Not available for this business.");
            }
        }

        return await next(context);
    }

    /// <summary>
    /// Unit of work: commits the request's transaction when the endpoint succeeded (status &lt; 400), before
    /// the response is written; otherwise the transaction rolls back when the scope ends.
    /// </summary>
    public static async ValueTask<object?> CommitOnSuccess(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var result = await next(context);
        var status = result is IStatusCodeHttpResult { StatusCode: { } code } ? code : StatusCodes.Status200OK;
        var uow = context.HttpContext.RequestServices.GetRequiredService<PostgresUnitOfWork>();
        if (status < 400 && uow.HasStarted)
        {
            await uow.CommitAsync(context.HttpContext.RequestAborted);
        }

        return result;
    }
}

using Microsoft.AspNetCore.Antiforgery;

namespace MenuX.Host.Api.Bff;

/// <summary>
/// CSRF protection for state-changing BFF and API requests (doc 09 §5): SameSite=Strict cookies plus an
/// anti-forgery token. The SPA reads the request token from the readable <see cref="RequestTokenCookie"/>
/// and sends it in <see cref="HeaderName"/> (Angular's HttpClient XSRF support).
/// </summary>
public static class AntiforgeryValidation
{
    public const string HeaderName = "X-XSRF-TOKEN";
    public const string RequestTokenCookie = "__Host-XSRF-TOKEN";

    /// <summary>Endpoint filter: rejects the request with 400 if the anti-forgery token is missing or invalid.</summary>
    public static async ValueTask<object?> RequireAntiforgeryToken(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        if (!await antiforgery.IsRequestValidAsync(context.HttpContext).ConfigureAwait(false))
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Missing or invalid anti-forgery token.");
        }

        return await next(context).ConfigureAwait(false);
    }

    /// <summary>Issues a fresh request token in a cookie the SPA can read (not HttpOnly by design).</summary>
    public static void IssueRequestToken(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        httpContext.Response.Cookies.Append(RequestTokenCookie, tokens.RequestToken ?? string.Empty, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
    }
}

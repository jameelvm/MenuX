using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace MenuX.Host.Api.Bff;

/// <summary>BFF endpoints for the admin SPA: login, logout and the current session.</summary>
public static class BffEndpoints
{
    public static IEndpointRouteBuilder MapBff(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var bff = app.MapGroup("/bff");

        // Starts the server-side OIDC login. Only local return URLs are accepted (no open redirect).
        bff.MapGet("/login", (string? returnUrl) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = IsLocalUrl(returnUrl) ? returnUrl : "/" },
                [OpenIdConnectDefaults.AuthenticationScheme]))
            .AllowAnonymous();

        // The SPA calls this on start: 200 with the session (and a fresh anti-forgery token) or 401.
        bff.MapGet("/session", (HttpContext context, ClaimsPrincipal user) =>
            {
                AntiforgeryValidation.IssueRequestToken(context);
                return Results.Ok(new SessionResponse(
                    user.FindFirstValue(BffAuthentication.IssuerClaim) ?? string.Empty,
                    user.FindFirstValue(BffAuthentication.SubjectClaim) ?? string.Empty));
            })
            .RequireAuthorization();

        // Ends the local session and the identity-provider session. State-changing: anti-forgery required.
        bff.MapPost("/logout", () =>
                Results.SignOut(
                    new AuthenticationProperties { RedirectUri = "/" },
                    [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .RequireAuthorization()
            .AddEndpointFilter(AntiforgeryValidation.RequireAntiforgeryToken);

        return app;
    }

    internal static bool IsLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url)
        && url[0] == '/'
        && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
        && !url.Contains("://", StringComparison.Ordinal);

    public sealed record SessionResponse(string Issuer, string Subject);
}

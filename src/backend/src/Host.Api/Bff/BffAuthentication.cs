using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace MenuX.Host.Api.Bff;

/// <summary>OIDC settings for the admin BFF (Keycloak locally, Cognito in AWS; ADR-06).</summary>
public sealed class OidcOptions
{
    public const string Section = "Oidc";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Only for local Keycloak over plain HTTP (Development).</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}

/// <summary>
/// Admin Backend-for-Frontend session (ADR-06, doc 09 §3): ASP.NET Core performs the OIDC authorization
/// code flow with PKCE server-side and issues an HttpOnly, Secure, SameSite=Strict cookie. The browser
/// never holds tokens. The session keeps only the issuer and subject; everything else comes from the
/// MenuX database (provider-neutral identity, X-2).
/// </summary>
public static class BffAuthentication
{
    public const string SessionCookieName = "__Host-menux-session";
    public const string IssuerClaim = "iss";
    public const string SubjectClaim = "sub";

    public static IServiceCollection AddMenuXBff(this IServiceCollection services, IConfiguration configuration)
    {
        // Bound lazily (options pattern) so configuration overrides, e.g. Cognito in AWS or tests, always apply.
        services.Configure<OidcOptions>(configuration.GetSection(OidcOptions.Section));
        services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure<IOptions<OidcOptions>>((options, oidc) =>
            {
                options.Authority = oidc.Value.Authority;
                options.ClientId = oidc.Value.ClientId;
                options.ClientSecret = oidc.Value.ClientSecret;
                options.RequireHttpsMetadata = oidc.Value.RequireHttpsMetadata;
            });

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // API callers are challenged by the cookie scheme (401); only /bff/login challenges OIDC.
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                // API callers get status codes, never redirects to the login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(options =>
            {
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.MapInboundClaims = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                // The handler deletes "iss" after validation by default; provider-neutral identity needs it
                // (users.idp_issuer + idp_subject, X-2), so keep it.
                options.ClaimActions.Remove(IssuerClaim);
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.Events.OnTokenValidated = context =>
                {
                    // Keep only iss + sub (ADR-06): the session carries no profile data or tokens.
                    var principal = context.Principal ?? throw new InvalidOperationException("No principal after token validation.");
                    var issuer = principal.FindFirstValue(IssuerClaim) ?? throw new InvalidOperationException("Token has no iss claim.");
                    var subject = principal.FindFirstValue(SubjectClaim) ?? throw new InvalidOperationException("Token has no sub claim.");
                    var identity = new ClaimsIdentity(
                        [new Claim(IssuerClaim, issuer), new Claim(SubjectClaim, subject)],
                        authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
                        nameType: SubjectClaim,
                        roleType: null);
                    context.Principal = new ClaimsPrincipal(identity);
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryValidation.HeaderName;
            options.Cookie.Name = "__Host-menux-antiforgery";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        return services;
    }
}

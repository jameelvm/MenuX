using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using MenuX.Host.Api.Bff;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.Keycloak;

[assembly: AssemblyFixture(typeof(MenuX.IntegrationTests.KeycloakFixture))]

namespace MenuX.IntegrationTests;

/// <summary>Local Keycloak with the same realm as Docker Compose (infrastructure/local/keycloak).</summary>
public sealed class KeycloakFixture : IAsyncLifetime
{
    private readonly KeycloakContainer _container = new KeycloakBuilder("keycloak/keycloak:26.8.2")
        .WithResourceMapping(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Keycloak", "menux-realm.json")), FilePath.Of("/opt/keycloak/data/import/menux-realm.json"))
        .WithCommand("--import-realm")
        .Build();

    public string Authority { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Authority = new Uri(new Uri(_container.GetBaseAddress()), "realms/menux").ToString();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

/// <summary>
/// Admin BFF session (ADR-06, doc 09 §3 and §5), end to end against real Keycloak: server-side code flow
/// with PKCE, an HttpOnly/Secure/SameSite=Strict session cookie holding only iss + sub, no tokens in
/// the browser, 401 (not redirects) for API calls, and anti-forgery on logout.
/// </summary>
public sealed partial class BffLoginTests(KeycloakFixture keycloak) : IDisposable
{
    private readonly WebApplicationFactory<OidcOptions> _factory = new WebApplicationFactory<OidcOptions>()
        .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Oidc:Authority"] = keycloak.Authority,
            ["Oidc:ClientId"] = "menux-admin-bff",
            ["Oidc:ClientSecret"] = "local-bff-secret-change-me",
            ["Oidc:RequireHttpsMetadata"] = "false",
        })));

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Session_without_a_cookie_returns_401_not_a_login_redirect()
    {
        using var app = CreateAppClient();
        var response = await app.GetAsync(new Uri("/bff/session", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_issues_a_secure_httponly_session_holding_only_issuer_and_subject()
    {
        using var app = CreateAppClient();
        var (signInResponse, _) = await LoginAsync(app, "owner.a@example.test", returnUrl: "/admin/menus");

        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/admin/menus", signInResponse.Headers.Location?.OriginalString);
        var sessionCookie = Assert.Single(signInResponse.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith(BffAuthentication.SessionCookieName + "=", StringComparison.Ordinal));
        Assert.Contains("httponly", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", sessionCookie, StringComparison.OrdinalIgnoreCase);

        var session = await app.GetFromJsonAsync<BffEndpoints.SessionResponse>(new Uri("/bff/session", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.NotNull(session);
        Assert.Equal(keycloak.Authority, session.Issuer.TrimEnd('/'));
        Assert.True(Guid.TryParse(session.Subject, out _), "Keycloak subjects are UUIDs");
    }

    [Fact]
    public async Task Logout_requires_the_antiforgery_token()
    {
        using var app = CreateAppClient();
        await LoginAsync(app, "editor.a@example.test", returnUrl: "/");
        var sessionResponse = await app.GetAsync(new Uri("/bff/session", UriKind.Relative), TestContext.Current.CancellationToken);
        var requestToken = ReadCookie(sessionResponse, AntiforgeryValidation.RequestTokenCookie);

        var withoutToken = await app.PostAsync(new Uri("/bff/logout", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, withoutToken.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/bff/logout", UriKind.Relative));
        request.Headers.Add(AntiforgeryValidation.HeaderName, requestToken);
        var withToken = await app.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, withToken.StatusCode);
        Assert.StartsWith(keycloak.Authority, withToken.Headers.Location?.ToString(), StringComparison.Ordinal);

        var afterLogout = await app.GetAsync(new Uri("/bff/session", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Theory]
    [InlineData("/admin", true)]
    [InlineData("/", true)]
    [InlineData("//evil.example/x", false)]
    [InlineData("/\\evil.example", false)]
    [InlineData("https://evil.example/", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_local_return_urls_are_accepted(string? url, bool expected) =>
        Assert.Equal(expected, BffEndpoints.IsLocalUrl(url));

    // ---------------------------------------------------------------- browser simulation

    private HttpClient CreateAppClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    /// <summary>
    /// Plays the browser: app /bff/login → Keycloak login form → credentials → form_post back to the
    /// app's /signin-oidc. Returns the app's response to the callback.
    /// </summary>
    private static async Task<(HttpResponseMessage SignIn, string Subject)> LoginAsync(HttpClient app, string username, string returnUrl)
    {
        var ct = TestContext.Current.CancellationToken;
        var challenge = await app.GetAsync(new Uri($"/bff/login?returnUrl={Uri.EscapeDataString(returnUrl)}", UriKind.Relative), ct);
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var authorizeUrl = challenge.Headers.Location!;
        // Pushed Authorization Request (PAR): the authorize parameters, including the PKCE challenge, were sent
        // to Keycloak over the back channel; the browser only sees a request_uri. PKCE itself is enforced by
        // the Keycloak client (pkce.code.challenge.method = S256), so the login below only succeeds with it.
        Assert.Contains("request_uri=urn", authorizeUrl.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("code_challenge=", authorizeUrl.Query, StringComparison.Ordinal);

        // Browsers send Secure cookies to http://localhost (a trustworthy origin); .NET's CookieContainer
        // doesn't, so the simulated browser uses a minimal localhost cookie jar for the identity provider.
        using var handler = new LocalhostCookieJar(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        using var browser = new HttpClient(handler);
        var loginPage = await GetFollowingRedirectsAsync(browser, authorizeUrl, ct);
        var action = WebUtility.HtmlDecode(LoginFormAction().Match(loginPage).Groups[1].Value);
        Assert.False(string.IsNullOrEmpty(action), "Keycloak login form not found");

        using var credentials = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = "local-dev-only",
            ["credentialId"] = string.Empty,
        });
        var formPost = await browser.PostAsync(new Uri(action), credentials, ct);
        var formPostHtml = await formPost.Content.ReadAsStringAsync(ct);

        // response_mode=form_post: Keycloak returns an auto-submitting form to the app's callback.
        var callback = new Uri(WebUtility.HtmlDecode(FormAction().Match(formPostHtml).Groups[1].Value));
        Assert.Equal("/signin-oidc", callback.AbsolutePath);
        var fields = HiddenInput().Matches(formPostHtml)
            .ToDictionary(m => m.Groups[1].Value, m => WebUtility.HtmlDecode(m.Groups[2].Value));
        Assert.Contains("code", fields.Keys);

        using var callbackForm = new FormUrlEncodedContent(fields);
        var signIn = await app.PostAsync(new Uri(callback.PathAndQuery, UriKind.Relative), callbackForm, ct);
        return (signIn, string.Empty);
    }

    /// <summary>Follows redirects that stay on the identity provider, like a browser would.</summary>
    private static async Task<string> GetFollowingRedirectsAsync(HttpClient browser, Uri url, CancellationToken ct)
    {
        for (var hop = 0; hop < 5; hop++)
        {
            var response = await browser.GetAsync(url, ct);
            if (response.StatusCode is not (HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect))
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"IdP returned {(int)response.StatusCode} for {url}: {body[..Math.Min(body.Length, 600)]}");
                }

                return body;
            }

            var next = new Uri(url, response.Headers.Location!);
            Assert.Equal(url.Authority, next.Authority);
            url = next;
        }

        throw new InvalidOperationException("Too many redirects at the identity provider.");
    }

    private static string ReadCookie(HttpResponseMessage response, string name)
    {
        var header = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(name + "=", StringComparison.Ordinal));
        return Uri.UnescapeDataString(header[(name.Length + 1)..header.IndexOf(';', StringComparison.Ordinal)]);
    }

    [GeneratedRegex(@"<form[^>]*id=""kc-form-login""[^>]*action=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex LoginFormAction();

    [GeneratedRegex(@"<form[^>]*action=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex FormAction();

    [GeneratedRegex(@"<input[^>]*name=""([^""]+)""[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex HiddenInput();
}

/// <summary>Minimal cookie jar for a single local host: stores name=value pairs and sends them back.</summary>
internal sealed class LocalhostCookieJar(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_cookies.Count > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(c => $"{c.Key}={c.Value}")));
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                var pair = header.Split(';', 2)[0];
                var separator = pair.IndexOf('=', StringComparison.Ordinal);
                var name = pair[..separator];
                var value = pair[(separator + 1)..];
                var expired = header.Contains("Max-Age=0", StringComparison.OrdinalIgnoreCase)
                    || header.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);
                if (expired || value.Length == 0)
                {
                    _cookies.Remove(name);
                }
                else
                {
                    _cookies[name] = value;
                }
            }
        }

        return response;
    }
}

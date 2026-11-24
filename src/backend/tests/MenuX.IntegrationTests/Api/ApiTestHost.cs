using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MenuX.Host.Api.Bff;
using MenuX.Host.Api.Tenancy;
using MenuX.Identity.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MenuX.IntegrationTests.Api;

/// <summary>
/// The real API host against the PostgreSQL test container. Authentication is replaced by a test scheme
/// (header X-Test-Sub = subject, fixed issuer) so API tests don't need Keycloak for every user; the real
/// login is covered end to end by BffLoginTests. Everything after authentication is production code.
/// </summary>
public sealed class ApiFactory(PostgresFixture db) : WebApplicationFactory<OidcOptions>
{
    public const string Issuer = "https://test.idp/realms/menux";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:App"] = db.App(),
            ["Oidc:Authority"] = "https://test.idp/realms/menux",
        }));
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = TestAuthHandler.SchemeName;
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    /// <summary>Creates (first login) a verified user and returns a client signed in as that user.</summary>
    public async Task<ApiClient> SignInAsync(string name)
    {
        var subject = $"{name}-{Guid.NewGuid():N}";
        using (var scope = Services.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<IIdentityApi>().EnsureUserAsync(
                new FirstLoginProfile(Issuer, subject, $"{subject}@example.test", EmailVerified: true, name));
            Assert.Equal(ProvisioningOutcome.Created, result.Outcome);
            var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject);
            var apiClient = new ApiClient(client, result.UserId!.Value);
            await apiClient.LoadAntiforgeryTokenAsync();
            return apiClient;
        }
    }
}

/// <summary>An HTTP client for one signed-in user, with the anti-forgery header and optional tenant selection.</summary>
public sealed class ApiClient(HttpClient http, Guid userId) : IDisposable
{
    private string _antiforgery = string.Empty;

    public Guid UserId { get; } = userId;

    public HttpClient Http { get; } = http;

    public async Task LoadAntiforgeryTokenAsync()
    {
        var response = await Http.GetAsync(new Uri("/bff/session", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        var header = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(AntiforgeryValidation.RequestTokenCookie + "=", StringComparison.Ordinal));
        _antiforgery = Uri.UnescapeDataString(header[(AntiforgeryValidation.RequestTokenCookie.Length + 1)..header.IndexOf(';', StringComparison.Ordinal)]);
    }

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, Guid? tenantId = null, object? body = null, bool antiforgery = true)
    {
        var request = new HttpRequestMessage(method, new Uri(url, UriKind.Relative));
        if (tenantId is not null)
        {
            request.Headers.Add(TenantResolutionMiddleware.TenantHeader, tenantId.ToString());
        }

        if (antiforgery && method != HttpMethod.Get)
        {
            request.Headers.Add(AntiforgeryValidation.HeaderName, _antiforgery);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return Http.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public async Task<T> GetJsonAsync<T>(string url, Guid? tenantId = null)
    {
        var response = await SendAsync(HttpMethod.Get, url, tenantId);
        Assert.True(response.IsSuccessStatusCode, $"GET {url}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>Creates a tenant as this user (who becomes its Owner) and returns its ID.</summary>
    public async Task<Guid> CreateTenantAsync(string? slug = null)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/tenants", body: new
        {
            slug = slug ?? $"t-{Guid.NewGuid():N}"[..20],
            displayName = "Test Bistro",
            businessType = "restaurant",
            countryCode = "US",
        });
        Assert.True(response.IsSuccessStatusCode, $"create tenant: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
    }

    public async Task<Guid> CreateLocationAsync(Guid tenantId, string? slug = null)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/locations", tenantId, new
        {
            slug = slug ?? $"loc-{Guid.NewGuid():N}"[..16],
            name = "Downtown",
            timezone = "America/New_York",
        });
        Assert.True(response.IsSuccessStatusCode, $"create location: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
    }

    public void Dispose() => Http.Dispose();

    public sealed record IdResponse(Guid Id);
}

internal sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string SubjectHeader = "X-Test-Sub";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = Request.Headers[SubjectHeader].ToString();
        if (string.IsNullOrEmpty(subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim(BffAuthentication.IssuerClaim, ApiFactory.Issuer), new Claim(BffAuthentication.SubjectClaim, subject)],
            SchemeName, BffAuthentication.SubjectClaim, null);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

/// <summary>Direct database helpers for test setup (as the app role, with explicit tenant context).</summary>
internal static class TestDb
{
    internal static readonly Guid AdminRole = Guid.Parse("0193a000-0000-7000-8000-000000000002");
    internal static readonly Guid EditorRole = Guid.Parse("0193a000-0000-7000-8000-000000000003");
    internal static readonly Guid StaffRole = Guid.Parse("0193a000-0000-7000-8000-000000000004");

    /// <summary>Adds a member with one role (and optional location scope); returns the membership ID.</summary>
    public static async Task<Guid> AddMemberAsync(PostgresFixture db, Guid tenantId, Guid userId, Guid roleId, Guid? scopedLocation = null)
    {
        var membershipId = Guid.CreateVersion7();
        await ExecAsync(db.App(), tenantId, $"""
            INSERT INTO identity.memberships (tenant_id, id, user_id, status) VALUES ('{tenantId}', '{membershipId}', '{userId}', 'active');
            INSERT INTO identity.membership_roles (tenant_id, membership_id, role_id) VALUES ('{tenantId}', '{membershipId}', '{roleId}');
            {(scopedLocation is null ? string.Empty : $"INSERT INTO identity.membership_locations (tenant_id, membership_id, location_id) VALUES ('{tenantId}', '{membershipId}', '{scopedLocation}');")}
            """);
        return membershipId;
    }

    public static async Task<Guid> MembershipIdAsync(PostgresFixture db, Guid tenantId, Guid userId)
    {
        await using var connection = new NpgsqlConnection(db.App());
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await SetTenantAsync(connection, tx, tenantId);
        await using var command = new NpgsqlCommand($"SELECT id FROM identity.memberships WHERE tenant_id = '{tenantId}' AND user_id = '{userId}'", connection, tx);
        return (Guid)(await command.ExecuteScalarAsync())!;
    }

    public static Task SetTenantStatusAsync(PostgresFixture db, Guid tenantId, string status) =>
        ExecAsync(db.App(), tenantId, $"UPDATE tenancy.tenants SET status = '{status}' WHERE id = '{tenantId}'");

    private static async Task ExecAsync(string connectionString, Guid tenantId, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await SetTenantAsync(connection, tx, tenantId);
        await using (var command = new NpgsqlCommand(sql, connection, tx))
        {
            await command.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    private static async Task SetTenantAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid tenantId)
    {
        await using var command = new NpgsqlCommand("SELECT set_config('app.tenant_id', @t, true)", connection, tx);
        command.Parameters.AddWithValue("t", tenantId.ToString());
        await command.ExecuteNonQueryAsync();
    }
}

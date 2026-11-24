using System.Net;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MenuX.IntegrationTests.Api;

/// <summary>
/// Protected test group 1 (doc 10 §12.2; doc 09 §4 layer L6): cross-tenant isolation for EVERY /api
/// endpoint, discovered from the running app, so new endpoints can't be forgotten.
///   1. A user of tenant B who selects tenant A gets 404 everywhere (existence not revealed).
///   2. A user working in tenant B who passes tenant A's resource IDs gets 404.
/// Endpoints with a route parameter that has no sample below fail the test until a case is added.
/// </summary>
public sealed class CrossTenantHarnessTests(PostgresFixture db) : IDisposable
{
    /// <summary>Endpoints that work without a selected tenant (the user's own data).</summary>
    private static readonly HashSet<string> TenantFree = new(StringComparer.Ordinal)
    {
        "GET /api/me", "GET /api/me/tenants", "POST /api/tenants",
    };

    private readonly ApiFactory _api = new(db);

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Every_endpoint_isolates_tenants()
    {
        using var ownerA = await _api.SignInAsync("ownerA");
        using var ownerB = await _api.SignInAsync("ownerB");
        var tenantA = await ownerA.CreateTenantAsync();
        var tenantB = await ownerB.CreateTenantAsync();
        var locationA = await ownerA.CreateLocationAsync(tenantA);
        await ownerB.CreateLocationAsync(tenantB);
        var membershipA = await TestDb.MembershipIdAsync(db, tenantA, ownerA.UserId);

        // Tenant A's resources, by route parameter name.
        var samples = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["locationId"] = locationA,
            ["membershipId"] = membershipA,
        };

        // Minimal valid bodies, so a 404 can't be masked by a 400.
        var bodies = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["PUT /api/tenant"] = new { displayName = "Hijacked" },
            ["POST /api/locations"] = new { slug = "x-loc", name = "X", timezone = "America/New_York" },
            ["PUT /api/locations/{locationId:guid}"] = new { name = "Hijacked" },
            ["PUT /api/members/{membershipId:guid}/roles"] = new { roleKeys = new[] { "editor" } },
        };

        var endpoints = _api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("/api", StringComparison.Ordinal) == true)
            .SelectMany(e => (e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["GET"])
                .Select(method => (Method: method, Route: e.RoutePattern.RawText!, Parameters: e.RoutePattern.Parameters.Select(p => p.Name).ToList())))
            .ToList();
        Assert.NotEmpty(endpoints);

        var failures = new List<string>();
        foreach (var (method, route, parameters) in endpoints)
        {
            var key = $"{method} {route}";
            if (TenantFree.Contains(key))
            {
                continue;
            }

            var missing = parameters.Where(p => !samples.ContainsKey(p)).ToList();
            if (missing.Count > 0)
            {
                failures.Add($"{key}: no cross-tenant sample for {string.Join(", ", missing)} (add one to this test)");
                continue;
            }

            var url = parameters.Aggregate(route, (u, p) => System.Text.RegularExpressions.Regex.Replace(u, "[{]" + p + "(:[^}]*)?[}]", samples[p].ToString()));
            bodies.TryGetValue(key, out var body);

            // 1. Outsider selects tenant A.
            var asOutsider = await ownerB.SendAsync(new HttpMethod(method), url, tenantA, body);
            if (asOutsider.StatusCode != HttpStatusCode.NotFound)
            {
                failures.Add($"{key} with X-Tenant-Id of another tenant: {(int)asOutsider.StatusCode} (expected 404)");
            }

            // 2. Own tenant B, tenant A's IDs.
            if (parameters.Count > 0)
            {
                var withForeignIds = await ownerB.SendAsync(new HttpMethod(method), url, tenantB, body);
                if (withForeignIds.StatusCode != HttpStatusCode.NotFound)
                {
                    failures.Add($"{key} with another tenant's IDs: {(int)withForeignIds.StatusCode} (expected 404)");
                }
            }
        }

        Assert.True(failures.Count == 0, "Cross-tenant failures:\n" + string.Join('\n', failures));

        // Tenant A is unchanged.
        var tenant = await ownerA.GetJsonAsync<MenuX.Tenancy.Endpoints.TenancyEndpoints.TenantResponse>("/api/tenant", tenantA);
        Assert.Equal("Test Bistro", tenant.DisplayName);
    }
}

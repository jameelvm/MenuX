using System.Net;
using MenuX.Identity.Endpoints;
using MenuX.Tenancy.Endpoints;

namespace MenuX.IntegrationTests.Api;

/// <summary>
/// Slice 3 behaviour through the real API: self-service tenant creation (creator becomes Owner), tenant
/// picker, permission checks, owner rules, location scope, tenant-status gate, anti-forgery and audit.
/// </summary>
public sealed class TenantApiTests(PostgresFixture db) : IDisposable
{
    private readonly ApiFactory _api = new(db);

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Creating_a_tenant_makes_the_creator_its_owner_and_writes_an_audit_event()
    {
        using var owner = await _api.SignInAsync("owner");
        var tenantId = await owner.CreateTenantAsync();

        var mine = await owner.GetJsonAsync<List<TenancyEndpoints.TenantSummary>>("/api/me/tenants");
        Assert.Contains(mine, t => t.Id == tenantId);

        var tenant = await owner.GetJsonAsync<TenancyEndpoints.TenantResponse>("/api/tenant", tenantId);
        Assert.Equal("active", tenant.Status);
        Assert.Equal("us-pilot", tenant.MarketPack);
        Assert.Equal("USD", tenant.DefaultCurrency);
        Assert.Equal("none", tenant.SubscriptionStatus); // billing rules pending (U7)

        var members = await owner.GetJsonAsync<List<IdentityEndpoints.MemberResponse>>("/api/members", tenantId);
        var me = Assert.Single(members);
        Assert.Equal(owner.UserId, me.UserId);
        Assert.Equal(["owner"], me.Roles);

        var audit = await owner.GetJsonAsync<List<MenuX.Audit.Endpoints.AuditEndpoints.AuditEventResponse>>("/api/audit", tenantId);
        Assert.Contains(audit, e => e.Action == "tenant.created" && e.ActorId == owner.UserId);
    }

    [Fact]
    public async Task Writes_without_the_antiforgery_token_are_rejected()
    {
        using var owner = await _api.SignInAsync("owner");
        var response = await owner.SendAsync(HttpMethod.Post, "/api/tenants", body: new { slug = "no-token", displayName = "X", businessType = "cafe", countryCode = "US" }, antiforgery: false);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Taken_and_reserved_slugs_are_rejected()
    {
        using var owner = await _api.SignInAsync("owner");
        var slug = $"bistro-{Guid.NewGuid():N}"[..20];
        await owner.CreateTenantAsync(slug);

        var taken = await owner.SendAsync(HttpMethod.Post, "/api/tenants", body: new { slug, displayName = "Copy", businessType = "restaurant", countryCode = "US" });
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);

        var reserved = await owner.SendAsync(HttpMethod.Post, "/api/tenants", body: new { slug = "admin", displayName = "Admin", businessType = "restaurant", countryCode = "US" });
        Assert.Equal(HttpStatusCode.BadRequest, reserved.StatusCode);
    }

    [Fact]
    public async Task A_tenant_you_dont_belong_to_looks_like_it_doesnt_exist()
    {
        using var ownerA = await _api.SignInAsync("ownerA");
        using var outsider = await _api.SignInAsync("outsider");
        var tenantA = await ownerA.CreateTenantAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await outsider.SendAsync(HttpMethod.Get, "/api/tenant", tenantA)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.SendAsync(HttpMethod.Get, "/api/tenant", Guid.CreateVersion7())).StatusCode);
        var picker = await outsider.GetJsonAsync<List<TenancyEndpoints.TenantSummary>>("/api/me/tenants");
        Assert.DoesNotContain(picker, t => t.Id == tenantA);
    }

    [Fact]
    public async Task Editors_cannot_manage_locations_or_members()
    {
        using var owner = await _api.SignInAsync("owner");
        using var editor = await _api.SignInAsync("editor");
        var tenantId = await owner.CreateTenantAsync();
        await TestDb.AddMemberAsync(db, tenantId, editor.UserId, TestDb.EditorRole);

        var create = await editor.SendAsync(HttpMethod.Post, "/api/locations", tenantId, new { slug = "bar", name = "Bar", timezone = "America/New_York" });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await editor.SendAsync(HttpMethod.Get, "/api/members", tenantId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await editor.SendAsync(HttpMethod.Get, "/api/locations", tenantId)).StatusCode);
    }

    [Fact]
    public async Task Admins_cannot_grant_owner_and_the_last_owner_cannot_be_removed()
    {
        using var owner = await _api.SignInAsync("owner");
        using var admin = await _api.SignInAsync("admin");
        var tenantId = await owner.CreateTenantAsync();
        var adminMembership = await TestDb.AddMemberAsync(db, tenantId, admin.UserId, TestDb.AdminRole);
        var ownerMembership = await TestDb.MembershipIdAsync(db, tenantId, owner.UserId);

        var selfPromotion = await admin.SendAsync(HttpMethod.Put, $"/api/members/{adminMembership}/roles", tenantId, new { roleKeys = new[] { "owner" } });
        Assert.Equal(HttpStatusCode.Forbidden, selfPromotion.StatusCode);

        var removeLastOwner = await owner.SendAsync(HttpMethod.Put, $"/api/members/{ownerMembership}/roles", tenantId, new { roleKeys = new[] { "admin" } });
        Assert.Equal(HttpStatusCode.Conflict, removeLastOwner.StatusCode);

        var members = await owner.GetJsonAsync<List<IdentityEndpoints.MemberResponse>>("/api/members", tenantId);
        Assert.Equal(["owner"], members.Single(m => m.MembershipId == ownerMembership).Roles); // rolled back

        var demoteAdmin = await owner.SendAsync(HttpMethod.Put, $"/api/members/{adminMembership}/roles", tenantId, new { roleKeys = new[] { "editor" } });
        Assert.Equal(HttpStatusCode.OK, demoteAdmin.StatusCode);
        var audit = await owner.GetJsonAsync<List<MenuX.Audit.Endpoints.AuditEndpoints.AuditEventResponse>>("/api/audit", tenantId);
        Assert.Contains(audit, e => e.Action == "member.roles_changed" && e.TargetId == adminMembership.ToString());
    }

    [Fact]
    public async Task Location_scope_limits_what_scoped_staff_can_see()
    {
        using var owner = await _api.SignInAsync("owner");
        using var staff = await _api.SignInAsync("staff");
        var tenantId = await owner.CreateTenantAsync();
        var downtown = await owner.CreateLocationAsync(tenantId);
        var airport = await owner.CreateLocationAsync(tenantId);
        await TestDb.AddMemberAsync(db, tenantId, staff.UserId, TestDb.StaffRole, scopedLocation: downtown);

        var visible = await staff.GetJsonAsync<List<TenancyEndpoints.LocationResponse>>("/api/locations", tenantId);
        Assert.Equal(downtown, Assert.Single(visible).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await staff.SendAsync(HttpMethod.Get, $"/api/locations/{airport}", tenantId)).StatusCode);
    }

    [Fact]
    public async Task A_suspended_tenant_is_read_only()
    {
        using var owner = await _api.SignInAsync("owner");
        var tenantId = await owner.CreateTenantAsync();
        await TestDb.SetTenantStatusAsync(db, tenantId, "suspended");

        Assert.Equal(HttpStatusCode.OK, (await owner.SendAsync(HttpMethod.Get, "/api/tenant", tenantId)).StatusCode);
        var write = await owner.SendAsync(HttpMethod.Post, "/api/locations", tenantId, new { slug = "late", name = "Late", timezone = "America/New_York" });
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task Failed_requests_roll_back_and_successful_ones_commit()
    {
        using var owner = await _api.SignInAsync("owner");
        var tenantId = await owner.CreateTenantAsync();
        var slug = $"dup-{Guid.NewGuid():N}"[..12];
        await owner.CreateLocationAsync(tenantId, slug);

        var duplicate = await owner.SendAsync(HttpMethod.Post, "/api/locations", tenantId, new { slug, name = "Dup", timezone = "America/New_York" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var locations = await owner.GetJsonAsync<List<TenancyEndpoints.LocationResponse>>("/api/locations", tenantId);
        Assert.Single(locations);
        var audit = await owner.GetJsonAsync<List<MenuX.Audit.Endpoints.AuditEndpoints.AuditEventResponse>>("/api/audit", tenantId);
        Assert.Single(audit, e => e.Action == "location.created");
    }

    [Fact]
    public async Task Unauthenticated_api_calls_get_401()
    {
        var anonymous = _api.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var response = await anonymous.GetAsync(new Uri("/api/me", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
    }
}

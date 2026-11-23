using MenuX.Host.Migrator;
using MenuX.Infrastructure.Persistence;
using MenuX.Infrastructure.Tenancy;
using MenuX.Tenancy.Domain;
using MenuX.Tenancy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MenuX.IntegrationTests;

/// <summary>
/// Protected test group 1 (doc 10 §12.2): PostgreSQL Row-Level Security. No tenant → zero rows;
/// no cross-tenant reads or writes; no leakage across pooled connections; the app role has no
/// BYPASSRLS and owns nothing. Isolation is proven at the database level (L4), independently of the
/// EF Core query filters (L3).
/// </summary>
public sealed class RowLevelSecurityTests(PostgresFixture db)
{
    [Fact]
    public async Task Migrations_are_idempotent()
    {
        var applied = await SqlMigrationRunner.ApplyAsync(db.OwnerConnectionString, MigrationCatalog.All, TestContext.Current.CancellationToken);
        Assert.Empty(applied);
    }

    [Fact]
    public async Task App_role_has_no_bypassrls_and_owns_no_tables()
    {
        await using var connection = await OpenAsync(db.OwnerConnectionString);
        Assert.False(await ScalarAsync<bool>(connection, "SELECT rolbypassrls FROM pg_roles WHERE rolname = 'menux_app'"));
        Assert.False(await ScalarAsync<bool>(connection, "SELECT pg_has_role('menux_app', 'menux_rls_definer', 'MEMBER')"));
        Assert.False(await ScalarAsync<bool>(connection, "SELECT pg_has_role('menux_app', 'menux_platform', 'MEMBER')"));
        Assert.Equal(0L, await ScalarAsync<long>(connection, """
            SELECT count(*) FROM pg_tables
            WHERE schemaname IN ('tenancy', 'identity', 'audit') AND tableowner = 'menux_app'
            """));
        Assert.Equal(0L, await ScalarAsync<long>(connection, """
            SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname IN ('tenancy', 'identity', 'audit') AND c.relkind IN ('r', 'p')
              AND NOT (c.relrowsecurity AND c.relforcerowsecurity)
              AND c.relispartition = false
            """));
    }

    [Fact]
    public async Task Without_a_tenant_no_rows_are_visible()
    {
        var (tenantA, _) = await SeedTenantAsync();
        await using var connection = await OpenAsync(db.App());

        // No transaction, no setting.
        Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM tenancy.tenants WHERE id = '{tenantA}'"));
        Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM tenancy.locations WHERE tenant_id = '{tenantA}'"));
        Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM identity.memberships WHERE tenant_id = '{tenantA}'"));
    }

    [Fact]
    public async Task A_tenant_sees_only_its_own_rows_even_without_ef_query_filters()
    {
        var (tenantA, locationA) = await SeedTenantAsync();
        var (tenantB, _) = await SeedTenantAsync();

        await using var context = CreateTenancyContext(tenantA);
        var ct = TestContext.Current.CancellationToken;
        var locations = await context.InTenantTransactionAsync(() => context.Locations.IgnoreQueryFilters().ToListAsync(ct), ct);
        var tenants = await context.InTenantTransactionAsync(() => context.Tenants.IgnoreQueryFilters().ToListAsync(ct), ct);

        Assert.All(locations, l => Assert.Equal(tenantA, l.TenantId));
        Assert.Contains(locations, l => l.Id == locationA);
        Assert.DoesNotContain(tenants, t => t.Id == tenantB);
        Assert.Equal(tenantA, Assert.Single(tenants).Id);
    }

    [Fact]
    public async Task Ef_reads_outside_a_transaction_fail_loudly_instead_of_returning_nothing()
    {
        var (tenantA, _) = await SeedTenantAsync();
        await using var context = CreateTenancyContext(tenantA);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Locations.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Contains("inside a transaction", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_single_row_insert_runs_in_a_transaction_so_rls_allows_it()
    {
        var (tenantA, _) = await SeedTenantAsync();
        await using var context = CreateTenancyContext(tenantA);
        context.Locations.Add(NewLocation(tenantA)); // one statement: EF would normally skip the transaction
        Assert.Equal(1, await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_tenant_cannot_write_rows_for_another_tenant()
    {
        var (tenantA, _) = await SeedTenantAsync();
        var (tenantB, _) = await SeedTenantAsync();

        await using var context = CreateTenancyContext(tenantA);
        context.Locations.Add(NewLocation(tenantB));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    [Fact]
    public async Task Tenant_setting_does_not_leak_across_pooled_connections()
    {
        var (tenantA, _) = await SeedTenantAsync();
        var singleConnection = db.App(maxPoolSize: 1);

        await using (var connection = await OpenAsync(singleConnection))
        await using (var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            await SetTenantAsync(connection, tx, tenantA, null);
            Assert.Equal(1L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM tenancy.tenants WHERE id = '{tenantA}'", tx));
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }

        // Same physical connection (pool of one): the SET LOCAL ended with the transaction.
        await using (var connection = await OpenAsync(singleConnection))
        {
            Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM tenancy.tenants WHERE id = '{tenantA}'"));
            await using var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
            Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM tenancy.tenants WHERE id = '{tenantA}'", tx));
        }
    }

    [Fact]
    public async Task Membership_roles_cannot_point_at_another_tenants_membership()
    {
        var (tenantA, _) = await SeedTenantAsync();
        var (tenantB, _) = await SeedTenantAsync();
        var (_, membershipB) = await SeedMembershipAsync(tenantB);

        await using var connection = await OpenAsync(db.App());
        await using var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await SetTenantAsync(connection, tx, tenantA, null);

        // Tenant A tries to attach a role to tenant B's membership under its own tenant_id: the composite FK rejects it.
        var error = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(connection, $"""
            INSERT INTO identity.membership_roles (tenant_id, membership_id, role_id)
            VALUES ('{tenantA}', '{membershipB}', '0193a000-0000-7000-8000-000000000004')
            """, tx));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
    }

    [Fact]
    public async Task Users_see_their_own_memberships_across_tenants_but_not_others()
    {
        var (tenantA, _) = await SeedTenantAsync();
        var (tenantB, _) = await SeedTenantAsync();
        var (userId, _) = await SeedMembershipAsync(tenantA);
        await SeedMembershipAsync(tenantB, userId);
        var (otherUser, _) = await SeedMembershipAsync(tenantB);

        await using var connection = await OpenAsync(db.App());
        await using var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await SetTenantAsync(connection, tx, null, userId);

        Assert.Equal(2L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM identity.memberships WHERE user_id = '{userId}'", tx));
        Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM identity.memberships WHERE user_id = '{otherUser}'", tx));
        Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM identity.users WHERE id = '{otherUser}'", tx));
    }

    [Fact]
    public async Task System_roles_are_shared_and_tenant_roles_are_private()
    {
        var (tenantA, _) = await SeedTenantAsync();
        var (tenantB, _) = await SeedTenantAsync();
        var customRole = Guid.CreateVersion7();
        await InTenantAsync(tenantA, null, $"INSERT INTO identity.roles (id, tenant_id, key, name) VALUES ('{customRole}', '{tenantA}', 'publisher', 'Publisher')");

        await using var connection = await OpenAsync(db.App());
        await using var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await SetTenantAsync(connection, tx, tenantB, null);

        Assert.Equal(4L, await ScalarAsync<long>(connection, "SELECT count(*) FROM identity.roles WHERE tenant_id IS NULL", tx));
        Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM identity.roles WHERE id = '{customRole}'", tx));
        var error = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(connection,
            "INSERT INTO identity.role_permissions (role_id, permission_key) VALUES ('0193a000-0000-7000-8000-000000000003', 'menu.publish')", tx));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task Audit_events_are_append_only_for_the_app_role()
    {
        var (tenantA, _) = await SeedTenantAsync();
        var eventId = Guid.CreateVersion7();
        await InTenantAsync(tenantA, null, $"INSERT INTO audit.audit_events (id, tenant_id, actor_type, action) VALUES ('{eventId}', '{tenantA}', 'system', 'test.event')");

        await using var connection = await OpenAsync(db.App());
        await using var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await SetTenantAsync(connection, tx, tenantA, null);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecAsync(connection, $"UPDATE audit.audit_events SET action = 'tampered' WHERE id = '{eventId}'", tx));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task Sign_in_lookup_finds_a_user_without_tenant_or_user_context()
    {
        var (tenantA, _) = await SeedTenantAsync();
        var (userId, _) = await SeedMembershipAsync(tenantA);

        await using var connection = await OpenAsync(db.App());
        var found = await ScalarAsync<Guid>(connection, $"SELECT identity.find_user_id('https://idp.test', 'sub-{userId}')");
        Assert.Equal(userId, found);
        Assert.Equal(0L, await ScalarAsync<long>(connection, $"SELECT count(*) FROM identity.users WHERE id = '{userId}'"));
    }

    // ---------------------------------------------------------------- helpers

    private TenancyDbContext CreateTenancyContext(Guid tenantId)
    {
        var tenantContext = new TenantContext();
        tenantContext.Apply(tenantId, null);
        var builder = new DbContextOptionsBuilder<TenancyDbContext>();
        builder.UseMenuXPostgres(db.App(), tenantContext);
        var options = builder.Options;
        return new TenancyDbContext(options, tenantContext);
    }

    private static Location NewLocation(Guid tenantId) => new()
    {
        TenantId = tenantId,
        Id = Guid.CreateVersion7(),
        Slug = $"loc-{Guid.NewGuid():N}",
        Name = "Downtown",
    };

    /// <summary>Creates a tenant and one location through EF Core in that tenant's context.</summary>
    private async Task<(Guid TenantId, Guid LocationId)> SeedTenantAsync()
    {
        var tenantId = Guid.CreateVersion7();
        await using var context = CreateTenancyContext(tenantId);
        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Slug = $"t-{tenantId:N}",
            LegalName = "Test Bistro LLC",
            DisplayName = "Test Bistro",
            Status = "active",
        });
        var location = NewLocation(tenantId);
        context.Locations.Add(location);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (tenantId, location.Id);
    }

    /// <summary>Creates (or reuses) a user and a membership in the tenant.</summary>
    private async Task<(Guid UserId, Guid MembershipId)> SeedMembershipAsync(Guid tenantId, Guid? existingUser = null)
    {
        var userId = existingUser ?? Guid.CreateVersion7();
        var membershipId = Guid.CreateVersion7();
        if (existingUser is null)
        {
            await InTenantAsync(null, userId, $"""
                INSERT INTO identity.users (id, idp_issuer, idp_subject, email, display_name, status)
                VALUES ('{userId}', 'https://idp.test', 'sub-{userId}', '{userId:N}@example.test', 'Test User', 'active')
                """);
        }

        await InTenantAsync(tenantId, null, $"""
            INSERT INTO identity.memberships (tenant_id, id, user_id, status) VALUES ('{tenantId}', '{membershipId}', '{userId}', 'active')
            """);
        return (userId, membershipId);
    }

    private async Task InTenantAsync(Guid? tenantId, Guid? userId, string sql)
    {
        await using var connection = await OpenAsync(db.App());
        await using var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await SetTenantAsync(connection, tx, tenantId, userId);
        await ExecAsync(connection, sql, tx);
        await tx.CommitAsync(TestContext.Current.CancellationToken);
    }

    private static async Task SetTenantAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid? tenantId, Guid? userId)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config('app.tenant_id', @t, true), set_config('app.user_id', @u, true)", connection, tx);
        command.Parameters.AddWithValue("t", tenantId?.ToString() ?? string.Empty);
        command.Parameters.AddWithValue("u", userId?.ToString() ?? string.Empty);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    private static async Task ExecAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? tx = null)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, NpgsqlTransaction? tx = null)
    {
        await using var command = new NpgsqlCommand(sql, connection, tx);
        return (T)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}

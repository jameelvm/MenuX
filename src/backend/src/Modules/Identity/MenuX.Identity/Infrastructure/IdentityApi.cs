using MenuX.Identity.Contracts;
using MenuX.Infrastructure.Persistence;
using MenuX.Infrastructure.Tenancy;
using Npgsql;

namespace MenuX.Identity.Infrastructure;

/// <summary>
/// Identity &amp; Access implementation over the <c>identity</c> schema (plain SQL; RLS applies through the
/// session settings). Login-time and middleware lookups run in their own short transactions, because
/// they happen before the request's tenant context exists; member changes run in the request unit of work.
/// </summary>
internal sealed class IdentityApi(NpgsqlDataSource dataSource, PostgresUnitOfWork unitOfWork) : IIdentityApi
{
    internal static readonly Guid OwnerRoleId = Guid.Parse("0193a000-0000-7000-8000-000000000001");

    public async Task<ProvisioningResult> EnsureUserAsync(FirstLoginProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // API-side enforcement (the identity provider enforces it too): no verified email, no MenuX user.
        if (!profile.EmailVerified || string.IsNullOrWhiteSpace(profile.Email))
        {
            return new ProvisioningResult(ProvisioningOutcome.EmailNotVerified, null);
        }

        var existing = await FindUserIdAsync(profile.Issuer, profile.Subject, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            // The database is the source of truth after first login: no profile update from the token.
            return new ProvisioningResult(ProvisioningOutcome.Existing, existing);
        }

        var userId = Guid.CreateVersion7();
        var (connection, transaction) = await TenantSession.BeginAsync(
            dataSource, new TenantContextValues(null, userId), cancellationToken).ConfigureAwait(false);
        await using (connection)
        await using (transaction)
        {
            await using var insert = new NpgsqlCommand("""
                INSERT INTO identity.users (id, idp_issuer, idp_subject, email, display_name, status, email_verified_at)
                VALUES (@id, @iss, @sub, @email, @name, 'active', now())
                ON CONFLICT (idp_issuer, idp_subject) DO NOTHING
                RETURNING id
                """, connection, transaction);
            insert.Parameters.AddWithValue("id", userId);
            insert.Parameters.AddWithValue("iss", profile.Issuer);
            insert.Parameters.AddWithValue("sub", profile.Subject);
            insert.Parameters.AddWithValue("email", profile.Email.Trim());
            insert.Parameters.AddWithValue("name", string.IsNullOrWhiteSpace(profile.Name) ? profile.Email.Trim() : profile.Name.Trim());

            try
            {
                var created = await insert.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (created is null)
                {
                    // (issuer, subject) exists but find_user_id didn't return it: the user is not active.
                    return new ProvisioningResult(ProvisioningOutcome.Disabled, null);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new ProvisioningResult(ProvisioningOutcome.Created, userId);
            }
            catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation && e.ConstraintName == "users_email_key")
            {
                return new ProvisioningResult(ProvisioningOutcome.EmailInUse, null);
            }
        }
    }

    public async Task<Guid?> FindUserIdAsync(string issuer, string subject, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand("SELECT identity.find_user_id(@iss, @sub)");
        command.Parameters.AddWithValue("iss", issuer);
        command.Parameters.AddWithValue("sub", subject);
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as Guid?;
    }

    public async Task<ResolvedMembership?> ResolveMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = await TenantSession.BeginAsync(
            dataSource, new TenantContextValues(tenantId, userId), cancellationToken).ConfigureAwait(false);
        await using (connection)
        await using (transaction)
        {
            await using var membershipCommand = new NpgsqlCommand(
                "SELECT id FROM identity.memberships WHERE tenant_id = @t AND user_id = @u AND status = 'active'",
                connection, transaction);
            membershipCommand.Parameters.AddWithValue("t", tenantId);
            membershipCommand.Parameters.AddWithValue("u", userId);
            if (await membershipCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not Guid membershipId)
            {
                return null;
            }

            var permissions = new HashSet<string>(StringComparer.Ordinal);
            await using (var permissionCommand = new NpgsqlCommand("""
                SELECT DISTINCT rp.permission_key
                FROM identity.membership_roles mr
                JOIN identity.role_permissions rp ON rp.role_id = mr.role_id
                WHERE mr.tenant_id = @t AND mr.membership_id = @m
                """, connection, transaction))
            {
                permissionCommand.Parameters.AddWithValue("t", tenantId);
                permissionCommand.Parameters.AddWithValue("m", membershipId);
                await using var reader = await permissionCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    permissions.Add(reader.GetString(0));
                }
            }

            var locations = new List<Guid>();
            await using (var locationCommand = new NpgsqlCommand(
                "SELECT location_id FROM identity.membership_locations WHERE tenant_id = @t AND membership_id = @m",
                connection, transaction))
            {
                locationCommand.Parameters.AddWithValue("t", tenantId);
                locationCommand.Parameters.AddWithValue("m", membershipId);
                await using var reader = await locationCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    locations.Add(reader.GetGuid(0));
                }
            }

            return new ResolvedMembership(tenantId, membershipId, permissions, locations.Count == 0 ? null : locations);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetMemberTenantIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = await TenantSession.BeginAsync(
            dataSource, new TenantContextValues(null, userId), cancellationToken).ConfigureAwait(false);
        await using (connection)
        await using (transaction)
        {
            await using var command = new NpgsqlCommand(
                "SELECT tenant_id FROM identity.memberships WHERE user_id = @u AND status = 'active' ORDER BY created_at",
                connection, transaction);
            command.Parameters.AddWithValue("u", userId);
            var ids = new List<Guid>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                ids.Add(reader.GetGuid(0));
            }

            return ids;
        }
    }

    public async Task<bool> HasVerifiedEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = await TenantSession.BeginAsync(
            dataSource, new TenantContextValues(null, userId), cancellationToken).ConfigureAwait(false);
        await using (connection)
        await using (transaction)
        {
            await using var command = new NpgsqlCommand(
                "SELECT email_verified_at IS NOT NULL FROM identity.users WHERE id = @u AND status = 'active'",
                connection, transaction);
            command.Parameters.AddWithValue("u", userId);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true;
        }
    }

    public async Task AddOwnerMembershipAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = unitOfWork.GetOrStart();
        var membershipId = Guid.CreateVersion7();
        await using var command = new NpgsqlCommand("""
            INSERT INTO identity.memberships (tenant_id, id, user_id, status) VALUES (@t, @m, @u, 'active');
            INSERT INTO identity.membership_roles (tenant_id, membership_id, role_id) VALUES (@t, @m, @owner);
            """, connection, transaction);
        command.Parameters.AddWithValue("t", tenantId);
        command.Parameters.AddWithValue("m", membershipId);
        command.Parameters.AddWithValue("u", userId);
        command.Parameters.AddWithValue("owner", OwnerRoleId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

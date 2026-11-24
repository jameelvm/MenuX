using MenuX.Audit.Contracts;
using MenuX.Identity.Contracts;
using MenuX.Infrastructure.Persistence;
using MenuX.Infrastructure.Tenancy;
using MenuX.Infrastructure.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Npgsql;

namespace MenuX.Identity.Endpoints;

/// <summary>Identity endpoints: the signed-in user's profile, tenant members and their roles.</summary>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        api.MapGet("/me", GetMeAsync);
        api.MapGet("/roles", ListRolesAsync).RequirePermission(Permissions.MemberManage);
        api.MapGet("/members", ListMembersAsync).RequirePermission(Permissions.MemberManage);
        api.MapPut("/members/{membershipId:guid}/roles", ReplaceRolesAsync).RequirePermission(Permissions.MemberManage);
        return api;
    }

    public sealed record MeResponse(Guid Id, string Email, string DisplayName);

    public sealed record MemberResponse(Guid MembershipId, string Status, Guid UserId, string Email, string DisplayName, IReadOnlyList<string> Roles);

    public sealed record RoleResponse(string Key, string Name, bool IsSystem);

    public sealed record ReplaceRolesRequest(IReadOnlyList<string>? RoleKeys);

    private static async Task<IResult> GetMeAsync(ITenantContext tenant, PostgresUnitOfWork uow, CancellationToken ct)
    {
        var (connection, transaction) = uow.GetOrStart();
        await using var command = new NpgsqlCommand("SELECT id, email, display_name FROM identity.users WHERE id = @u", connection, transaction);
        command.Parameters.AddWithValue("u", tenant.UserId ?? Guid.Empty);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? Results.Ok(new MeResponse(reader.GetGuid(0), reader.GetString(1), reader.GetString(2)))
            : Results.NotFound();
    }

    private static async Task<IResult> ListRolesAsync(PostgresUnitOfWork uow, CancellationToken ct)
    {
        var (connection, transaction) = uow.GetOrStart();
        await using var command = new NpgsqlCommand(
            "SELECT key, name, tenant_id IS NULL FROM identity.roles ORDER BY tenant_id NULLS FIRST, key", connection, transaction);
        var roles = new List<RoleResponse>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            roles.Add(new RoleResponse(reader.GetString(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        return Results.Ok(roles);
    }

    private static async Task<IResult> ListMembersAsync(ITenantContext tenant, PostgresUnitOfWork uow, CancellationToken ct)
    {
        var (connection, transaction) = uow.GetOrStart();
        await using var command = new NpgsqlCommand("""
            SELECT m.id, m.status, u.id, u.email, u.display_name,
                   coalesce(array_agg(r.key ORDER BY r.key) FILTER (WHERE r.key IS NOT NULL), '{}')
            FROM identity.memberships m
            JOIN identity.users u ON u.id = m.user_id
            LEFT JOIN identity.membership_roles mr ON mr.tenant_id = m.tenant_id AND mr.membership_id = m.id
            LEFT JOIN identity.roles r ON r.id = mr.role_id
            WHERE m.tenant_id = @t
            GROUP BY m.id, m.status, u.id, u.email, u.display_name
            ORDER BY u.display_name
            """, connection, transaction);
        command.Parameters.AddWithValue("t", tenant.TenantId!.Value);
        var members = new List<MemberResponse>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            members.Add(new MemberResponse(
                reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetString(3), reader.GetString(4),
                reader.GetFieldValue<string[]>(5)));
        }

        return Results.Ok(members);
    }

    /// <summary>
    /// Replaces a member's roles. Rules [D]: at least one role; only callers with the owner-only
    /// <c>tenant.delete</c> permission can grant or remove the Owner role (no self-promotion by admins);
    /// the tenant must keep at least one active owner (doc 08 invariant). Violations roll back.
    /// </summary>
    private static async Task<IResult> ReplaceRolesAsync(
        Guid membershipId, ReplaceRolesRequest request, ITenantContext tenant, PostgresUnitOfWork uow, IAuditLog audit, CancellationToken ct)
    {
        var requested = (request.RoleKeys ?? []).Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).Distinct(StringComparer.Ordinal).ToList();
        if (requested.Count == 0)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "A member needs at least one role.");
        }

        var tenantId = tenant.TenantId!.Value;
        var (connection, transaction) = uow.GetOrStart();

        var current = new List<string>();
        await using (var currentCommand = new NpgsqlCommand("""
            SELECT r.key FROM identity.memberships m
            LEFT JOIN identity.membership_roles mr ON mr.tenant_id = m.tenant_id AND mr.membership_id = m.id
            LEFT JOIN identity.roles r ON r.id = mr.role_id
            WHERE m.tenant_id = @t AND m.id = @m
            """, connection, transaction))
        {
            currentCommand.Parameters.AddWithValue("t", tenantId);
            currentCommand.Parameters.AddWithValue("m", membershipId);
            var found = false;
            await using var reader = await currentCommand.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                found = true;
                if (!reader.IsDBNull(0))
                {
                    current.Add(reader.GetString(0));
                }
            }

            if (!found)
            {
                return Results.NotFound();
            }
        }

        var roleIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        await using (var rolesCommand = new NpgsqlCommand("SELECT key, id FROM identity.roles WHERE key = ANY(@keys)", connection, transaction))
        {
            rolesCommand.Parameters.AddWithValue("keys", requested.ToArray());
            await using var reader = await rolesCommand.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                roleIds[reader.GetString(0)] = reader.GetGuid(1);
            }
        }

        var unknown = requested.Where(k => !roleIds.ContainsKey(k)).ToList();
        if (unknown.Count > 0)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Unknown role(s): {string.Join(", ", unknown)}.");
        }

        var ownerChanges = current.Contains(Roles.Owner) != requested.Contains(Roles.Owner);
        if (ownerChanges && !tenant.Permissions.Contains(Permissions.TenantDelete))
        {
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Only an owner can grant or remove the Owner role.");
        }

        await using (var replace = new NpgsqlCommand("""
            DELETE FROM identity.membership_roles WHERE tenant_id = @t AND membership_id = @m;
            INSERT INTO identity.membership_roles (tenant_id, membership_id, role_id) SELECT @t, @m, unnest(@roles);
            """, connection, transaction))
        {
            replace.Parameters.AddWithValue("t", tenantId);
            replace.Parameters.AddWithValue("m", membershipId);
            replace.Parameters.AddWithValue("roles", requested.Select(k => roleIds[k]).ToArray());
            await replace.ExecuteNonQueryAsync(ct);
        }

        await using (var owners = new NpgsqlCommand("""
            SELECT count(*) FROM identity.memberships m
            JOIN identity.membership_roles mr ON mr.tenant_id = m.tenant_id AND mr.membership_id = m.id
            WHERE m.tenant_id = @t AND m.status = 'active' AND mr.role_id = @owner
            """, connection, transaction))
        {
            owners.Parameters.AddWithValue("t", tenantId);
            owners.Parameters.AddWithValue("owner", Infrastructure.IdentityApi.OwnerRoleId);
            if ((long)(await owners.ExecuteScalarAsync(ct))! == 0)
            {
                return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "The tenant must keep at least one active owner.");
            }
        }

        await audit.RecordAsync(new AuditEntry("member.roles_changed", "membership", membershipId.ToString(),
            new Dictionary<string, string?> { ["before"] = string.Join(',', current.Order()), ["after"] = string.Join(',', requested.Order()) }), ct);

        return Results.Ok(new { roles = requested.Order().ToList() });
    }
}

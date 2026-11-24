using MenuX.Identity.Contracts;
using MenuX.Infrastructure.Persistence;
using MenuX.Infrastructure.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Npgsql;

namespace MenuX.Audit.Endpoints;

/// <summary>Tenant audit trail (newest first). Needs <c>audit.read</c>; RLS limits rows to the selected tenant.</summary>
public static class AuditEndpoints
{
    public sealed record AuditEventResponse(
        Guid Id, DateTimeOffset OccurredAt, string ActorType, Guid? ActorId, string Action, string? TargetType, string? TargetId, string Metadata);

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        api.MapGet("/audit", ListAsync).RequirePermission(Permissions.AuditRead);
        return api;
    }

    private static async Task<IResult> ListAsync(PostgresUnitOfWork uow, int? limit, CancellationToken ct)
    {
        var (connection, transaction) = uow.GetOrStart();
        await using var command = new NpgsqlCommand("""
            SELECT id, occurred_at, actor_type, actor_id, action, target_type, target_id, metadata::text
            FROM audit.audit_events ORDER BY occurred_at DESC LIMIT @limit
            """, connection, transaction);
        command.Parameters.AddWithValue("limit", Math.Clamp(limit ?? 50, 1, 200));
        var events = new List<AuditEventResponse>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            events.Add(new AuditEventResponse(
                reader.GetGuid(0), reader.GetFieldValue<DateTimeOffset>(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3), reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetString(7)));
        }

        return Results.Ok(events);
    }
}

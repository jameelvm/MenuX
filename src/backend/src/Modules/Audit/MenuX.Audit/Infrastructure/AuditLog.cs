using System.Diagnostics;
using System.Text.Json;
using MenuX.Audit.Contracts;
using MenuX.Infrastructure.Persistence;
using MenuX.Infrastructure.Tenancy;
using Npgsql;
using NpgsqlTypes;

namespace MenuX.Audit.Infrastructure;

/// <summary>
/// Writes audit events in the request's unit of work, so an event exists exactly when its change commits.
/// The tenant and actor come from the tenant context, never from the caller.
/// </summary>
internal sealed class AuditLog(PostgresUnitOfWork unitOfWork, ITenantContext tenant) : IAuditLog
{
    public async Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var tenantId = tenant.TenantId ?? throw new InvalidOperationException("Audit events need a selected tenant.");
        var (connection, transaction) = unitOfWork.GetOrStart();
        await using var command = new NpgsqlCommand("""
            INSERT INTO audit.audit_events (id, tenant_id, actor_type, actor_id, action, target_type, target_id, correlation_id, metadata)
            VALUES (@id, @tenant, @actorType, @actor, @action, @targetType, @targetId, @correlation, @metadata)
            """, connection, transaction);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("actorType", tenant.UserId is null ? "system" : "user");
        command.Parameters.AddWithValue("actor", (object?)tenant.UserId ?? DBNull.Value);
        command.Parameters.AddWithValue("action", entry.Action);
        command.Parameters.AddWithValue("targetType", (object?)entry.TargetType ?? DBNull.Value);
        command.Parameters.AddWithValue("targetId", (object?)entry.TargetId ?? DBNull.Value);
        command.Parameters.AddWithValue("correlation", (object?)Activity.Current?.TraceId.ToString() ?? DBNull.Value);
        command.Parameters.Add(new NpgsqlParameter("metadata", NpgsqlDbType.Jsonb)
        {
            Value = JsonSerializer.Serialize(entry.Metadata ?? new Dictionary<string, string?>()),
        });
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

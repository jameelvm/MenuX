namespace MenuX.Audit.Contracts;

/// <summary>An audit event for the selected tenant, written by the current user (doc 08 §4.7).</summary>
public sealed record AuditEntry(string Action, string? TargetType = null, string? TargetId = null, IReadOnlyDictionary<string, string?>? Metadata = null);

/// <summary>Audit public API: records events in the same transaction as the change (append-only).</summary>
public interface IAuditLog
{
    Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}

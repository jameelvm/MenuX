using MenuX.Infrastructure.Tenancy;
using Npgsql;

namespace MenuX.Infrastructure.Persistence;

/// <summary>
/// The single place that turns tenant-context values into PostgreSQL session settings for Row-Level
/// Security: <c>app.tenant_id</c>, <c>app.user_id</c> and <c>app.member_tenant_ids</c>, all with
/// <c>is_local = true</c> (= SET LOCAL), so they end with the transaction (doc 09 §4.2).
/// </summary>
public static class TenantSession
{
    public const string SetConfigSql =
        "SELECT set_config('app.tenant_id', @tenant_id, true), " +
        "set_config('app.user_id', @user_id, true), " +
        "set_config('app.member_tenant_ids', @member_tenant_ids, true)";

    /// <summary>Opens a connection and a transaction with the session settings applied.</summary>
    public static async Task<(NpgsqlConnection Connection, NpgsqlTransaction Transaction)> BeginAsync(
        NpgsqlDataSource dataSource, TenantContextValues values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await ApplyAsync(connection, transaction, values, cancellationToken).ConfigureAwait(false);
            return (connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public static async Task ApplyAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, TenantContextValues values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        await using var command = new NpgsqlCommand(SetConfigSql, connection, transaction);
        AddParameters(command, values);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static void AddParameters(System.Data.Common.DbCommand command, TenantContextValues values)
    {
        Add(command, "tenant_id", values.TenantId?.ToString() ?? string.Empty);
        Add(command, "user_id", values.UserId?.ToString() ?? string.Empty);
        Add(command, "member_tenant_ids", values.MemberTenantIds is { Count: > 0 } ids ? string.Join(',', ids) : string.Empty);
    }

    public static TenantContextValues From(ITenantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new TenantContextValues(context.TenantId, context.UserId, context.Permissions, context.LocationScope, context.MemberTenantIds);
    }

    private static void Add(System.Data.Common.DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

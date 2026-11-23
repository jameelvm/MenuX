using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace MenuX.Infrastructure.Persistence;

/// <summary>A module's SQL migrations: embedded resources whose names start with <see cref="ResourcePrefix"/>.</summary>
public sealed record ModuleMigrations(string Module, Assembly Assembly, string ResourcePrefix);

/// <summary>
/// Applies SQL-first migrations (schemas, tables, grants, Row-Level Security policies) as the schema
/// owner role. Run as a separate deployment step, never by the application role (doc 09 §4.2).
/// Each script runs once, in its own transaction, in name order per module; applied scripts are
/// recorded with a checksum, and a changed script fails the run.
/// </summary>
public static class SqlMigrationRunner
{
    private const string Bootstrap = """
        CREATE SCHEMA IF NOT EXISTS migrations;
        CREATE TABLE IF NOT EXISTS migrations.applied_scripts (
            module      text        NOT NULL,
            script      text        NOT NULL,
            checksum    text        NOT NULL,
            applied_at  timestamptz NOT NULL DEFAULT now(),
            PRIMARY KEY (module, script));
        """;

    public static async Task<IReadOnlyList<string>> ApplyAsync(
        string ownerConnectionString, IEnumerable<ModuleMigrations> modules, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modules);
        var applied = new List<string>();

        await using var connection = new NpgsqlConnection(ownerConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (var bootstrap = new NpgsqlCommand(Bootstrap, connection))
        {
            await bootstrap.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var module in modules)
        {
            foreach (var (script, sql) in ReadScripts(module))
            {
                var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
                var existing = await GetChecksumAsync(connection, module.Module, script, cancellationToken).ConfigureAwait(false);
                if (existing is not null)
                {
                    if (!string.Equals(existing, checksum, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Migration {module.Module}/{script} changed after it was applied. Add a new script instead.");
                    }

                    continue;
                }

                await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                await using (var command = new NpgsqlCommand(sql, connection, transaction))
                {
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await using (var record = new NpgsqlCommand(
                    "INSERT INTO migrations.applied_scripts (module, script, checksum) VALUES (@m, @s, @c)",
                    connection, transaction))
                {
                    record.Parameters.AddWithValue("m", module.Module);
                    record.Parameters.AddWithValue("s", script);
                    record.Parameters.AddWithValue("c", checksum);
                    await record.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                applied.Add($"{module.Module}/{script}");
            }
        }

        return applied;
    }

    private static IEnumerable<(string Script, string Sql)> ReadScripts(ModuleMigrations module)
    {
        var names = module.Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(module.ResourcePrefix, StringComparison.Ordinal)
                && n.EndsWith(".sql", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (names.Count == 0)
        {
            throw new InvalidOperationException($"No migration scripts found for module '{module.Module}'.");
        }

        foreach (var name in names)
        {
            using var stream = module.Assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Resource {name} not found.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            yield return (name[module.ResourcePrefix.Length..], reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal));
        }
    }

    private static async Task<string?> GetChecksumAsync(
        NpgsqlConnection connection, string module, string script, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT checksum FROM migrations.applied_scripts WHERE module = @m AND script = @s", connection);
        command.Parameters.AddWithValue("m", module);
        command.Parameters.AddWithValue("s", script);
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
    }
}

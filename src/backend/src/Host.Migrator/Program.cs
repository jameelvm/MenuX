using MenuX.Audit.Infrastructure;
using MenuX.Host.Migrator;
using MenuX.Identity.Infrastructure;
using MenuX.Infrastructure.Persistence;
using MenuX.Tenancy.Infrastructure;

// Usage: set MENUX_OWNER_CONNECTION (schema-owner role), then run this project.
var connectionString = Environment.GetEnvironmentVariable("MENUX_OWNER_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    await Console.Error.WriteLineAsync("MENUX_OWNER_CONNECTION is not set (connection string for the schema-owner role).");
    return 2;
}

var applied = await SqlMigrationRunner.ApplyAsync(connectionString, MigrationCatalog.All);
Console.WriteLine(applied.Count == 0 ? "Database is up to date." : $"Applied: {string.Join(", ", applied)}");
return 0;

namespace MenuX.Host.Migrator
{
    /// <summary>Module migrations in dependency order.</summary>
    public static class MigrationCatalog
    {
        public static IReadOnlyList<ModuleMigrations> All { get; } =
        [
            TenancyMigrations.Definition,
            IdentityMigrations.Definition,
            AuditMigrations.Definition,
        ];
    }
}

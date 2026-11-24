using MenuX.Host.Migrator;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(MenuX.IntegrationTests.PostgresFixture))]

namespace MenuX.IntegrationTests;

/// <summary>
/// One PostgreSQL container per test run, prepared like local Docker: the same three roles as
/// infrastructure/local/postgres/01-roles.sh, then all module migrations applied as the owner.
/// Image matches compose.yaml (same major as RDS).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string OwnerPassword = "test-owner";
    private const string AppPassword = "test-app";
    private const string PlatformPassword = "test-platform";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithDatabase("menux")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string OwnerConnectionString { get; private set; } = string.Empty;

    public string AppConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        var admin = _container.GetConnectionString();

        await using (var connection = new NpgsqlConnection(admin))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"""
                CREATE EXTENSION IF NOT EXISTS citext;
                CREATE ROLE menux_owner    LOGIN PASSWORD '{OwnerPassword}';
                CREATE ROLE menux_app      LOGIN NOBYPASSRLS PASSWORD '{AppPassword}';
                CREATE ROLE menux_platform LOGIN BYPASSRLS PASSWORD '{PlatformPassword}';
                CREATE ROLE menux_rls_definer NOLOGIN BYPASSRLS;
                GRANT menux_rls_definer TO menux_owner;
                GRANT CONNECT ON DATABASE menux TO menux_owner, menux_app, menux_platform;
                GRANT CREATE ON DATABASE menux TO menux_owner;
                REVOKE CREATE ON SCHEMA public FROM PUBLIC;
                """, connection);
            await command.ExecuteNonQueryAsync();
        }

        OwnerConnectionString = WithUser(admin, "menux_owner", OwnerPassword);
        AppConnectionString = WithUser(admin, "menux_app", AppPassword);
        await MenuX.Infrastructure.Persistence.SqlMigrationRunner.ApplyAsync(OwnerConnectionString, MigrationCatalog.All);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>App-role connection string with its own pool (optionally a single connection).</summary>
    public string App(int? maxPoolSize = null, string? applicationName = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(AppConnectionString)
        {
            ApplicationName = applicationName ?? Guid.NewGuid().ToString("N"),
        };
        if (maxPoolSize is not null)
        {
            builder.MaxPoolSize = maxPoolSize.Value;
        }

        return builder.ConnectionString;
    }

    private static string WithUser(string connectionString, string user, string password) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Username = user, Password = password }.ConnectionString;
}

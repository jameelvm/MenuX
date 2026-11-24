using MenuX.Infrastructure.Persistence;

namespace MenuX.Tenancy.Infrastructure;

/// <summary>The Tenancy module's SQL migrations (schema `tenancy`).</summary>
public static class TenancyMigrations
{
    public static ModuleMigrations Definition { get; } =
        new("tenancy", typeof(TenancyMigrations).Assembly, "MenuX.Tenancy.Migrations.");
}

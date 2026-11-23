using MenuX.Infrastructure.Persistence;

namespace MenuX.Identity.Infrastructure;

/// <summary>The Identity module's SQL migrations (schema `identity`).</summary>
public static class IdentityMigrations
{
    public static ModuleMigrations Definition { get; } =
        new("identity", typeof(IdentityMigrations).Assembly, "MenuX.Identity.Migrations.");
}

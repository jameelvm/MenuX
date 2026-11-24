using MenuX.Infrastructure.Persistence;

namespace MenuX.Audit.Infrastructure;

/// <summary>The Audit module's SQL migrations (schema `audit`).</summary>
public static class AuditMigrations
{
    public static ModuleMigrations Definition { get; } =
        new("audit", typeof(AuditMigrations).Assembly, "MenuX.Audit.Migrations.");
}

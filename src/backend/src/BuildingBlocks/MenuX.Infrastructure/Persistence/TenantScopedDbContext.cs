using Microsoft.EntityFrameworkCore;

namespace MenuX.Infrastructure.Persistence;

/// <summary>
/// Base class for module DbContexts on tenant-isolated PostgreSQL schemas. EF Core normally skips the
/// transaction when SaveChanges sends a single statement; here every SaveChanges uses one, so the
/// tenant session settings (<c>SET LOCAL app.tenant_id</c>) always apply and Row-Level Security sees them.
/// </summary>
public abstract class TenantScopedDbContext : DbContext
{
    protected TenantScopedDbContext(DbContextOptions options)
        : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
    }
}

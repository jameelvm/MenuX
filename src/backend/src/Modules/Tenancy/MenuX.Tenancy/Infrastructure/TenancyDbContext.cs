using MenuX.Infrastructure.Persistence;
using MenuX.Infrastructure.Tenancy;
using MenuX.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;

namespace MenuX.Tenancy.Infrastructure;

/// <summary>
/// EF Core context for the <c>tenancy</c> schema. Two isolation layers: a global query filter on the
/// tenant (L3) and PostgreSQL Row-Level Security via the session interceptor (L4, doc 09 §4).
/// </summary>
public sealed class TenancyDbContext(DbContextOptions<TenancyDbContext> options, ITenantContext tenantContext)
    : TenantScopedDbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Location> Locations => Set<Location>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("tenancy");

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(t => t.Id);
            // Selected tenant, or (tenant picker) the user's member tenants; RLS enforces the same in PostgreSQL.
            e.HasQueryFilter(t => t.Id == tenantContext.TenantId || tenantContext.MemberTenantIds.Contains(t.Id));
        });

        modelBuilder.Entity<Location>(e =>
        {
            e.ToTable("locations");
            e.HasKey(l => new { l.TenantId, l.Id });
            e.HasOne<Tenant>().WithMany().HasForeignKey(l => l.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(l => l.TenantId == tenantContext.TenantId);
        });
    }
}

namespace MenuX.Tenancy.Domain;

/// <summary>A venue or outlet of a tenant. Hotel outlets are locations; no hierarchy above the tenant (U4).</summary>
public sealed class Location
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LocationType { get; set; }
    public string Timezone { get; set; } = "America/New_York";
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

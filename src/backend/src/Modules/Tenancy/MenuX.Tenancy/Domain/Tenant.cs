namespace MenuX.Tenancy.Domain;

/// <summary>A business using MenuX (doc 08 §4.1). Tenant-owned rows are isolated by Row-Level Security.</summary>
public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = "restaurant";
    public string CountryCode { get; set; } = "US";
    public string DefaultCurrency { get; set; } = "USD";
    public string DefaultLanguage { get; set; } = "en-US";
    public string[] SupportedLanguages { get; set; } = ["en-US"];
    public string Status { get; set; } = "pending";
    public string PlanKey { get; set; } = "pilot";
    public string MarketPack { get; set; } = "us-pilot";
    public string HomeRegion { get; set; } = "us-east-1";

    /// <summary>Commercial state mirror (U7; billing rules pending). 'none' until billing exists.</summary>
    public string SubscriptionStatus { get; set; } = "none";
    public DateTimeOffset? TrialEndsAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

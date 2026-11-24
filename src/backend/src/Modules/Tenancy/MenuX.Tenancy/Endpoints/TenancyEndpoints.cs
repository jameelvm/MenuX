using System.Text.RegularExpressions;
using MenuX.Audit.Contracts;
using MenuX.Identity.Contracts;
using MenuX.Infrastructure.Persistence;
using MenuX.Infrastructure.Tenancy;
using MenuX.Infrastructure.Web;
using MenuX.Tenancy.Domain;
using MenuX.Tenancy.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MenuX.Tenancy.Endpoints;

/// <summary>Tenancy settings. HomeRegion: ADR-17 is still Proposed; the default is an assumption for local/test use.</summary>
public sealed class TenancyOptions
{
    public const string Section = "Tenancy";

    public string HomeRegion { get; set; } = "us-east-1";
}

/// <summary>Tenant self-service signup, tenant profile and locations (FR-TEN-01, FR-TEN-04).</summary>
public static partial class TenancyEndpoints
{
    // [D] Reserved slugs: words used by MenuX routes or likely to mislead guests.
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "app", "bff", "www", "menux", "support", "help", "static", "assets", "q", "m", "login", "signup",
    };

    private static readonly HashSet<string> BusinessTypes = new(StringComparer.Ordinal) { "restaurant", "hotel", "cafe", "shop", "other" };

    public static IEndpointRouteBuilder MapTenancyEndpoints(this IEndpointRouteBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        api.MapGet("/me/tenants", ListMyTenantsAsync);
        api.MapPost("/tenants", CreateTenantAsync);
        api.MapGet("/tenant", GetTenantAsync).RequireTenant();
        api.MapPut("/tenant", UpdateTenantAsync).RequirePermission(Permissions.TenantManage);
        api.MapGet("/locations", ListLocationsAsync).RequireTenant();
        api.MapGet("/locations/{locationId:guid}", GetLocationAsync).RequireTenant();
        api.MapPost("/locations", CreateLocationAsync).RequirePermission(Permissions.LocationManage);
        api.MapPut("/locations/{locationId:guid}", UpdateLocationAsync).RequirePermission(Permissions.LocationManage);
        return api;
    }

    public sealed record TenantSummary(Guid Id, string Slug, string DisplayName, string Status);

    public sealed record TenantResponse(
        Guid Id, string Slug, string LegalName, string DisplayName, string BusinessType, string CountryCode,
        string DefaultCurrency, string DefaultLanguage, IReadOnlyList<string> SupportedLanguages, string Status,
        string PlanKey, string MarketPack, string SubscriptionStatus, DateTimeOffset? TrialEndsAt);

    public sealed record CreateTenantRequest(
        string? Slug, string? DisplayName, string? LegalName, string? BusinessType, string? CountryCode, IReadOnlyList<string>? SupportedLanguages);

    public sealed record UpdateTenantRequest(string? DisplayName, string? LegalName, IReadOnlyList<string>? SupportedLanguages);

    public sealed record LocationResponse(Guid Id, string Slug, string Name, string? LocationType, string Timezone, string Currency, string Status);

    public sealed record CreateLocationRequest(string? Slug, string? Name, string? LocationType, string? Timezone, string? Currency);

    public sealed record UpdateLocationRequest(string? Name, string? LocationType, string? Timezone, string? Status);

    // ---------------------------------------------------------------- tenants

    /// <summary>Tenant picker: the tenants where the signed-in user has an active membership.</summary>
    private static async Task<IResult> ListMyTenantsAsync(TenancyDbContext db, CancellationToken ct)
    {
        var tenants = await db.Tenants.OrderBy(t => t.DisplayName)
            .Select(t => new TenantSummary(t.Id, t.Slug, t.DisplayName, t.Status)).ToListAsync(ct);
        return Results.Ok(tenants);
    }

    /// <summary>
    /// Self-service signup (product owner, 2026-11-23): a signed-in user with a verified email creates a
    /// tenant and becomes its Owner. Tenant, owner membership and audit event commit in one transaction.
    /// </summary>
    private static async Task<IResult> CreateTenantAsync(
        CreateTenantRequest request, ITenantContext current, IIdentityApi identity, IServiceScopeFactory scopes,
        IOptions<TenancyOptions> options, CancellationToken ct)
    {
        if (current.UserId is not Guid userId)
        {
            return Results.Unauthorized();
        }

        if (current.TenantId is not null)
        {
            return Problem(StatusCodes.Status400BadRequest, "Don't send X-Tenant-Id when creating a tenant.");
        }

        var errors = ValidateCreate(request);
        if (errors is not null)
        {
            return Results.ValidationProblem(errors);
        }

        // API-side email verification check (the identity provider enforces it as well).
        if (!await identity.HasVerifiedEmailAsync(userId, ct))
        {
            return Problem(StatusCodes.Status403Forbidden, "Verify your email address before creating a business.");
        }

        var pack = MarketPack.For(request.CountryCode!);
        var tenantId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant
        {
            Id = tenantId,
            Slug = request.Slug!.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName!.Trim(),
            LegalName = string.IsNullOrWhiteSpace(request.LegalName) ? request.DisplayName!.Trim() : request.LegalName.Trim(),
            BusinessType = request.BusinessType!,
            CountryCode = pack.CountryCode,
            DefaultCurrency = pack.Currency,
            DefaultLanguage = pack.DefaultLanguage,
            SupportedLanguages = NormalizeLanguages(request.SupportedLanguages, pack),
            Status = "active",
            PlanKey = "pilot",
            MarketPack = pack.Key,
            HomeRegion = options.Value.HomeRegion,
            SubscriptionStatus = "none",
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // The new tenant gets its own unit of work, with the tenant context set to the new tenant, so
        // Row-Level Security admits exactly these inserts.
        await using var scope = scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContextSetter>().Apply(new TenantContextValues(tenantId, userId));
        var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        db.Tenants.Add(tenant);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Problem(StatusCodes.Status409Conflict, "That web address (slug) is already taken.");
        }

        await scope.ServiceProvider.GetRequiredService<IIdentityApi>().AddOwnerMembershipAsync(tenantId, userId, ct);
        await scope.ServiceProvider.GetRequiredService<IAuditLog>().RecordAsync(
            new AuditEntry("tenant.created", "tenant", tenantId.ToString(),
                new Dictionary<string, string?> { ["slug"] = tenant.Slug, ["marketPack"] = tenant.MarketPack }), ct);
        await scope.ServiceProvider.GetRequiredService<PostgresUnitOfWork>().CommitAsync(ct);

        return Results.Created("/api/tenant", ToResponse(tenant));
    }

    private static async Task<IResult> GetTenantAsync(TenancyDbContext db, ITenantContext tenant, CancellationToken ct)
    {
        var entity = await db.Tenants.SingleOrDefaultAsync(t => t.Id == tenant.TenantId, ct);
        return entity is null ? Results.NotFound() : Results.Ok(ToResponse(entity));
    }

    private static async Task<IResult> UpdateTenantAsync(
        UpdateTenantRequest request, TenancyDbContext db, ITenantContext tenant, IAuditLog audit, CancellationToken ct)
    {
        var entity = await db.Tenants.SingleOrDefaultAsync(t => t.Id == tenant.TenantId, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        var errors = new Dictionary<string, string[]>();
        if (request.DisplayName is not null && !IsLength(request.DisplayName, 1, 100))
        {
            errors["displayName"] = ["1–100 characters."];
        }

        if (request.LegalName is not null && !IsLength(request.LegalName, 1, 200))
        {
            errors["legalName"] = ["1–200 characters."];
        }

        if (request.SupportedLanguages is not null)
        {
            var languages = request.SupportedLanguages.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).Distinct(StringComparer.Ordinal).ToList();
            if (languages.Count is < 1 or > 5 || !languages.Contains(entity.DefaultLanguage, StringComparer.Ordinal))
            {
                errors["supportedLanguages"] = [$"1–5 languages, including the default language {entity.DefaultLanguage} (U5)."];
            }
            else
            {
                entity.SupportedLanguages = [.. languages];
            }
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        entity.DisplayName = request.DisplayName?.Trim() ?? entity.DisplayName;
        entity.LegalName = request.LegalName?.Trim() ?? entity.LegalName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.RecordAsync(new AuditEntry("tenant.updated", "tenant", entity.Id.ToString()), ct);
        return Results.Ok(ToResponse(entity));
    }

    // ---------------------------------------------------------------- locations

    private static async Task<IResult> ListLocationsAsync(TenancyDbContext db, ITenantContext tenant, CancellationToken ct)
    {
        var query = db.Locations.AsQueryable();
        if (tenant.LocationScope is { } scope)
        {
            query = query.Where(l => scope.Contains(l.Id));
        }

        var locations = await query.OrderBy(l => l.Name).ToListAsync(ct);
        return Results.Ok(locations.Select(ToResponse));
    }

    private static async Task<IResult> GetLocationAsync(Guid locationId, TenancyDbContext db, ITenantContext tenant, CancellationToken ct)
    {
        if (!tenant.CanAccessLocation(locationId))
        {
            return Results.NotFound();
        }

        var location = await db.Locations.SingleOrDefaultAsync(l => l.Id == locationId, ct);
        return location is null ? Results.NotFound() : Results.Ok(ToResponse(location));
    }

    private static async Task<IResult> CreateLocationAsync(
        CreateLocationRequest request, TenancyDbContext db, ITenantContext tenant, IAuditLog audit, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (!IsSlug(request.Slug))
        {
            errors["slug"] = ["3–50 characters: lowercase letters, digits and hyphens."];
        }

        if (request.Name is null || !IsLength(request.Name, 1, 100))
        {
            errors["name"] = ["1–100 characters."];
        }

        if (!IsTimezone(request.Timezone))
        {
            errors["timezone"] = ["An IANA time zone such as America/New_York."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var parent = await db.Tenants.SingleAsync(t => t.Id == tenant.TenantId, ct);
        var now = DateTimeOffset.UtcNow;
        var location = new Location
        {
            TenantId = parent.Id,
            Id = Guid.CreateVersion7(),
            Slug = request.Slug!.Trim().ToLowerInvariant(),
            Name = request.Name!.Trim(),
            LocationType = string.IsNullOrWhiteSpace(request.LocationType) ? null : request.LocationType.Trim(),
            Timezone = request.Timezone!,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? parent.DefaultCurrency : request.Currency.Trim().ToUpperInvariant(),
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Locations.Add(location);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Problem(StatusCodes.Status409Conflict, "A location with that slug already exists.");
        }

        await audit.RecordAsync(new AuditEntry("location.created", "location", location.Id.ToString(),
            new Dictionary<string, string?> { ["slug"] = location.Slug }), ct);
        return Results.Created($"/api/locations/{location.Id}", ToResponse(location));
    }

    private static async Task<IResult> UpdateLocationAsync(
        Guid locationId, UpdateLocationRequest request, TenancyDbContext db, ITenantContext tenant, IAuditLog audit, CancellationToken ct)
    {
        if (!tenant.CanAccessLocation(locationId))
        {
            return Results.NotFound();
        }

        var location = await db.Locations.SingleOrDefaultAsync(l => l.Id == locationId, ct);
        if (location is null)
        {
            return Results.NotFound();
        }

        var errors = new Dictionary<string, string[]>();
        if (request.Name is not null && !IsLength(request.Name, 1, 100))
        {
            errors["name"] = ["1–100 characters."];
        }

        if (request.Timezone is not null && !IsTimezone(request.Timezone))
        {
            errors["timezone"] = ["An IANA time zone such as America/New_York."];
        }

        if (request.Status is not null && request.Status is not ("active" or "inactive"))
        {
            errors["status"] = ["active or inactive."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        location.Name = request.Name?.Trim() ?? location.Name;
        location.LocationType = request.LocationType is null ? location.LocationType : (request.LocationType.Trim() is { Length: > 0 } t ? t : null);
        location.Timezone = request.Timezone ?? location.Timezone;
        location.Status = request.Status ?? location.Status;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.RecordAsync(new AuditEntry("location.updated", "location", location.Id.ToString()), ct);
        return Results.Ok(ToResponse(location));
    }

    // ---------------------------------------------------------------- helpers

    private static Dictionary<string, string[]>? ValidateCreate(CreateTenantRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!IsSlug(request.Slug) || ReservedSlugs.Contains(request.Slug!.Trim()))
        {
            errors["slug"] = ["3–50 characters: lowercase letters, digits and hyphens; not a reserved word."];
        }

        if (request.DisplayName is null || !IsLength(request.DisplayName, 1, 100))
        {
            errors["displayName"] = ["1–100 characters."];
        }

        if (request.LegalName is not null && request.LegalName.Length > 0 && !IsLength(request.LegalName, 1, 200))
        {
            errors["legalName"] = ["Up to 200 characters."];
        }

        if (request.BusinessType is null || !BusinessTypes.Contains(request.BusinessType))
        {
            errors["businessType"] = ["restaurant, hotel, cafe, shop or other."];
        }

        if (request.CountryCode is null || !MarketPack.IsSupported(request.CountryCode))
        {
            errors["countryCode"] = ["Supported countries: US (pilot), AE."];
        }

        return errors.Count == 0 ? null : errors;
    }

    private static string[] NormalizeLanguages(IReadOnlyList<string>? requested, MarketPack pack)
    {
        var languages = (requested ?? pack.Languages).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim())
            .Prepend(pack.DefaultLanguage).Distinct(StringComparer.Ordinal).Take(5).ToArray();
        return languages;
    }

    private static bool IsSlug(string? slug) => slug is not null && SlugPattern().IsMatch(slug.Trim());

    private static bool IsLength(string value, int min, int max) => value.Trim().Length >= min && value.Trim().Length <= max;

    private static bool IsTimezone(string? timezone) =>
        !string.IsNullOrWhiteSpace(timezone) && timezone.Contains('/', StringComparison.Ordinal)
        && TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);

    private static IResult Problem(int status, string title) => Results.Problem(statusCode: status, title: title);

    private static TenantResponse ToResponse(Tenant t) => new(
        t.Id, t.Slug, t.LegalName, t.DisplayName, t.BusinessType, t.CountryCode, t.DefaultCurrency, t.DefaultLanguage,
        t.SupportedLanguages, t.Status, t.PlanKey, t.MarketPack, t.SubscriptionStatus, t.TrialEndsAt);

    private static LocationResponse ToResponse(Location l) => new(l.Id, l.Slug, l.Name, l.LocationType, l.Timezone, l.Currency, l.Status);

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{1,48})[a-z0-9]$")]
    private static partial Regex SlugPattern();
}

/// <summary>
/// Market-pack defaults per country (ADR-19, Proposed; doc 14 F7): US pilot pack and UAE pack. Only the
/// defaults needed for tenant creation; the full packs (allergen schemes etc.) arrive with the catalog.
/// </summary>
internal sealed record MarketPack(string Key, string CountryCode, string Currency, string DefaultLanguage, IReadOnlyList<string> Languages)
{
    private static readonly MarketPack Us = new("us-pilot", "US", "USD", "en-US", ["en-US"]);
    private static readonly MarketPack Ae = new("ae", "AE", "AED", "ar-AE", ["ar-AE", "en"]);

    public static bool IsSupported(string countryCode) => countryCode is "US" or "AE";

    public static MarketPack For(string countryCode) => countryCode switch
    {
        "US" => Us,
        "AE" => Ae,
        _ => throw new ArgumentOutOfRangeException(nameof(countryCode), countryCode, "Unsupported country."),
    };
}

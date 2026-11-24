using MenuX.Tenancy;
using MenuX.Host.Api.Bff;
using MenuX.Host.Api.Tenancy;
using MenuX.Infrastructure.Persistence;
using MenuX.Tenancy.Endpoints;
using MenuX.Identity.Endpoints;
using MenuX.Audit.Endpoints;
using MenuX.Identity;
using MenuX.Catalog;
using MenuX.Menus;
using MenuX.Publishing;
using MenuX.Media;
using MenuX.Notifications;
using MenuX.Audit;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL as the application role (no BYPASSRLS), one transaction per request (doc 09 §4.2).
builder.Services.AddMenuXPersistence(sp =>
    sp.GetRequiredService<IConfiguration>().GetConnectionString("App")
    ?? throw new InvalidOperationException("ConnectionStrings:App is not configured."));
builder.Services.Configure<TenancyOptions>(builder.Configuration.GetSection(TenancyOptions.Section));
builder.Services.AddMemoryCache();

builder.Services
    .AddTenancyModule()
    .AddIdentityModule()
    .AddCatalogModule()
    .AddMenusModule()
    .AddPublishingModule()
    .AddMediaModule()
    .AddNotificationsModule()
    .AddAuditModule();

// Admin BFF: server-side OIDC (Keycloak locally, Cognito in AWS) + HttpOnly session cookie (ADR-06).
builder.Services.AddMenuXBff(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();

// Liveness only.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapBff();

// Admin API: authenticated; writes need the anti-forgery token and pass the tenant-status gate;
// one transaction per request, committed only on success.
var api = app.MapGroup("/api")
    .RequireAuthorization()
    .AddEndpointFilter(ApiPipelineFilters.GuardWrites)
    .AddEndpointFilter(ApiPipelineFilters.CommitOnSuccess);
api.MapTenancyEndpoints();
api.MapIdentityEndpoints();
api.MapAuditEndpoints();

app.Run();

/// <summary>Entry point; public so integration tests can use WebApplicationFactory.</summary>
public partial class Program;

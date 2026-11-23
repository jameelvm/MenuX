using MenuX.Tenancy;
using MenuX.Host.Api.Bff;
using MenuX.Identity;
using MenuX.Catalog;
using MenuX.Menus;
using MenuX.Publishing;
using MenuX.Media;
using MenuX.Notifications;
using MenuX.Audit;

var builder = WebApplication.CreateBuilder(args);

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

// Liveness only.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapBff();

app.Run();

/// <summary>Entry point; public so integration tests can use WebApplicationFactory.</summary>
public partial class Program;

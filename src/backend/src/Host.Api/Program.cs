using MenuX.Tenancy;
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

var app = builder.Build();

// Liveness only. No business endpoints in week 1.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

/// <summary>Entry point; public so integration tests can use WebApplicationFactory.</summary>
public partial class Program;

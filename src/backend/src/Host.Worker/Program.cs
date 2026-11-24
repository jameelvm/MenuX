using MenuX.Tenancy;
using MenuX.Identity;
using MenuX.Catalog;
using MenuX.Menus;
using MenuX.Publishing;
using MenuX.Media;
using MenuX.Notifications;
using MenuX.Audit;

var builder = Host.CreateApplicationBuilder(args);

// Work-item pollers and outbox dispatch are added in later weeks (ADR-18 is still Proposed).
builder.Services
    .AddTenancyModule()
    .AddIdentityModule()
    .AddCatalogModule()
    .AddMenusModule()
    .AddPublishingModule()
    .AddMediaModule()
    .AddNotificationsModule()
    .AddAuditModule();

var host = builder.Build();
host.Run();

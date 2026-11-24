using System.Reflection;

namespace MenuX.ArchitectureTests;

/// <summary>The MVP-1 modules and the assemblies that make up the solution.</summary>
internal static class Modules
{
    /// <summary>
    /// MVP-1 modules (doc 05 §1). AI Assist (phase 2), Ordering, Payments, Billing and Analytics
    /// are not created in MVP-1.
    /// </summary>
    public static readonly string[] Names =
    [
        "Tenancy", "Identity", "Catalog", "Menus", "Publishing", "Media", "Notifications", "Audit",
    ];

    public static TheoryData<string> All()
    {
        var data = new TheoryData<string>();
        foreach (var name in Names)
        {
            data.Add(name);
        }

        return data;
    }

    /// <summary>Every assembly in the solution, loaded through a marker type.</summary>
    public static readonly Assembly[] Assemblies =
    [
        typeof(SharedKernel.SharedKernelAssembly).Assembly,
        typeof(Infrastructure.InfrastructureAssembly).Assembly,
        Assembly.Load("MenuX.Host.Api"),
        Assembly.Load("MenuX.Host.Worker"),
        Assembly.Load("MenuX.Host.Migrator"),
        typeof(Tenancy.TenancyModule).Assembly,
        typeof(Tenancy.Contracts.TenancyContractsAssembly).Assembly,
        typeof(Identity.IdentityModule).Assembly,
        typeof(Identity.Contracts.IdentityContractsAssembly).Assembly,
        typeof(Catalog.CatalogModule).Assembly,
        typeof(Catalog.Contracts.CatalogContractsAssembly).Assembly,
        typeof(Menus.MenusModule).Assembly,
        typeof(Menus.Contracts.MenusContractsAssembly).Assembly,
        typeof(Publishing.PublishingModule).Assembly,
        typeof(Publishing.Contracts.PublishingContractsAssembly).Assembly,
        typeof(Media.MediaModule).Assembly,
        typeof(Media.Contracts.MediaContractsAssembly).Assembly,
        typeof(Notifications.NotificationsModule).Assembly,
        typeof(Notifications.Contracts.NotificationsContractsAssembly).Assembly,
        typeof(Audit.AuditModule).Assembly,
        typeof(Audit.Contracts.AuditContractsAssembly).Assembly,
    ];

    /// <summary>Namespace regex for a module's internals (everything except its Contracts).</summary>
    public static string InternalsPattern(IEnumerable<string> modules) =>
        $@"^MenuX\.({string.Join('|', modules)})(?!\.Contracts)(\..*)?$";

    /// <summary>Namespace regex for a module, including its Contracts.</summary>
    public static string ModulePattern(string module) => $@"^MenuX\.{module}(\..*)?$";
}

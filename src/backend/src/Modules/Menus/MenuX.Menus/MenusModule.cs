using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Menus;

/// <summary>Registers the Menus module with the host. Empty in week 1.</summary>
public static class MenusModule
{
    public static IServiceCollection AddMenusModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}

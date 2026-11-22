using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Identity;

/// <summary>Registers the Identity module with the host. Empty in week 1.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}

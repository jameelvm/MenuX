using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Tenancy;

/// <summary>Registers the Tenancy module with the host. Empty in week 1.</summary>
public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}

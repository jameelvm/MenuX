using MenuX.Identity.Contracts;
using MenuX.Identity.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Identity;

/// <summary>Registers the Identity module. Needs AddMenuXPersistence (data source, unit of work) in the host.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IIdentityApi, IdentityApi>();
        return services;
    }
}

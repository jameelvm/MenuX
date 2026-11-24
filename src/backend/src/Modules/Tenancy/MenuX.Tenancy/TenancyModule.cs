using MenuX.Infrastructure.Persistence;
using MenuX.Tenancy.Contracts;
using MenuX.Tenancy.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Tenancy;

/// <summary>Registers the Tenancy module. Needs AddMenuXPersistence in the host.</summary>
public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTenantScopedDbContext<TenancyDbContext>((options, tenant) => new TenancyDbContext(options, tenant));
        services.AddScoped<ITenancyApi, TenancyApi>();
        services.AddSingleton<ITenantAccessPolicy, BillingRulesPendingAccessPolicy>();
        return services;
    }
}

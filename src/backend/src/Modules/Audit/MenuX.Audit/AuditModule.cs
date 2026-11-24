using MenuX.Audit.Contracts;
using MenuX.Audit.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Audit;

/// <summary>Registers the Audit module. Needs AddMenuXPersistence in the host.</summary>
public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IAuditLog, AuditLog>();
        return services;
    }
}

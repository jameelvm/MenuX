using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Audit;

/// <summary>Registers the Audit module with the host. Empty in week 1.</summary>
public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}

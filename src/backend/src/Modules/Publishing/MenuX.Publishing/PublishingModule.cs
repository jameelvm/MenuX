using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Publishing;

/// <summary>Registers the Publishing module with the host. Empty in week 1.</summary>
public static class PublishingModule
{
    public static IServiceCollection AddPublishingModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}

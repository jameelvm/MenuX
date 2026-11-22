using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Media;

/// <summary>Registers the Media module with the host. Empty in week 1.</summary>
public static class MediaModule
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}

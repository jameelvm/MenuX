using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Notifications;

/// <summary>Registers the Notifications module with the host. Empty in week 1.</summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}

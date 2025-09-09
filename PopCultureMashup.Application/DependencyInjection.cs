using Microsoft.Extensions.DependencyInjection;

namespace PopCultureMashup.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases / handlers
        services.AddScoped<SeedItemsHandler>();
        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;
using PopCultureMashup.Application.UseCases.Items;
using PopCultureMashup.Application.UseCases.Seed;

namespace PopCultureMashup.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {

        services.AddScoped<ISeedItemsHandler, SeedItemsHandler>();
        services.AddScoped<ISearchItemsHandler, SearchItemHandler>();
        return services;
    }
}
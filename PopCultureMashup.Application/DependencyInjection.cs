using Microsoft.Extensions.DependencyInjection;
using PopCultureMashup.Application.Abstractions;
using PopCultureMashup.Application.Services;
using PopCultureMashup.Application.UseCases.Items;
using PopCultureMashup.Application.UseCases.Recommend;
using PopCultureMashup.Application.UseCases.Seed;

namespace PopCultureMashup.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISeedItemsHandler, SeedItemsHandler>();
        services.AddScoped<ISearchItemsHandler, SearchItemHandler>();
        services.AddScoped<GenerateRecommendationsHandler>();
        services.AddScoped<GetRecommendationHandler>();
        services.AddSingleton<IRecommendationRanker, RecommendationRanker>();
        return services;
    }
}
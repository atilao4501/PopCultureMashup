using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.External;
using PopCultureMashup.Infrastructure.Persistence.Repositories;

namespace PopCultureMashup.Infrastructure.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalClients(this IServiceCollection services, IConfiguration config)
    {
        // Options
        services.Configure<RawgOptions>(config.GetSection("External:Rawg"));
        services.Configure<OpenLibraryOptions>(config.GetSection("External:OpenLibrary"));

        // Polly
        var retry = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, a =>
                TimeSpan.FromSeconds(Math.Pow(2, a)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 150)));

        var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(15));

        var circuit = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));

        // RAWG (typed client)
        services.AddHttpClient<IRawgClient, RawgClient>((sp, http) =>
            {
                var opt = sp.GetRequiredService<IOptions<RawgOptions>>().Value;
                http.BaseAddress = new Uri(opt.BaseUrl); // e.g. https://api.rawg.io/api/
                http.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(retry)
            .AddPolicyHandler(circuit)
            .AddPolicyHandler(timeout);

        // OpenLibrary (typed client)
        services.AddHttpClient<IOpenLibraryClient, OpenLibraryClient>((sp, http) =>
            {
                var opt = sp.GetRequiredService<IOptions<OpenLibraryOptions>>().Value;
                http.BaseAddress = new Uri(opt.BaseUrl); // e.g. https://openlibrary.org/
                http.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(retry)
            .AddPolicyHandler(circuit)
            .AddPolicyHandler(timeout);

        return services;
    }
    
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ISeedRepository, SeedRepository>();
        // depois: RecommendationRepository, FeedbackRepository etc
        return services;
    }
}
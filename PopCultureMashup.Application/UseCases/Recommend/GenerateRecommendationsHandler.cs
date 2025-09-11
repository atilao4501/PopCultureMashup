using Microsoft.Extensions.Logging;
using PopCultureMashup.Application.Abstractions;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.UseCases.Recommend;

public sealed class GenerateRecommendationsHandler(
    ISeedRepository repository,
    IItemRepository itemRepository,
    IRawgClient rawgClient,
    IOpenLibraryClient openLibClient,
    ILogger<GenerateRecommendationsHandler> logger,
    IRecommendationRanker ranker)
{
    public async Task<GenerateRecommendationsDTOs.GenerateRecommendationsResponse> HandleAsync(
        GenerateRecommendationsDTOs.GenerateRecommendationsRequest request,
        CancellationToken ct = default)
    {
        var userSeedsFromDb = await repository.GetByUserIdAsync(request.UserId, ct);
        if (!userSeedsFromDb.Any())
            throw new InvalidOperationException("No seeds found for the user.");

        var allThemes = userSeedsFromDb.SelectMany(s => s.Item.Themes)
            .Select(t => t.Slug)
            .Distinct()
            .ToList();

        var allGenres = userSeedsFromDb.SelectMany(s => s.Item.Genres)
            .Select(g => g.Genre)
            .Distinct()
            .ToList();

        var allCreators = userSeedsFromDb.SelectMany(s => s.Item.Creators)
            .Select(c => c.Slug ?? c.CreatorName)
            .Distinct()
            .ToList();

        Task<IReadOnlyList<Item>>? gamesTask = rawgClient.DiscoverGamesAsync(allGenres, allThemes, ct: ct);
        Task<IReadOnlyList<Item>>? booksTask = openLibClient.DiscoverBooksAsync(allThemes, allCreators, ct: ct);


        // Task<IReadOnlyList<Item>>? gamesTask = null;
        // Task<IReadOnlyList<Item>>? booksTask = null;
        //
        // // games
        // if (gameSeeds.Any())
        // {
        //     var gameGenres = gameSeeds.SelectMany(s => s.Item.Genres).Select(g => g.Genre).Distinct();
        //     var gameTags = gameSeeds.SelectMany(s => s.Item.Themes).Select(t => t.Theme).Distinct();
        //     gamesTask = rawgClient.DiscoverGamesAsync(gameGenres, gameTags, ct: ct);
        // }
        //
        // // books
        // if (bookSeeds.Any())
        // {
        //     var subjects = bookSeeds.SelectMany(s => s.Item.Themes).Select(t => t.Theme).Distinct();
        //     var authors = bookSeeds.SelectMany(s => s.Item.Creators).Select(c => c.CreatorName).Distinct();
        //     booksTask = openLibClient.DiscoverBooksAsync(subjects, authors, ct: ct);
        // }

        var tasks = new List<Task<IReadOnlyList<Item>>>();
        tasks.Add(gamesTask);
        tasks.Add(booksTask);

        if (tasks.Count == 0)
            throw new InvalidOperationException("No eligible seeds to generate recommendations.");

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Exceptions are handled below per task
        }

        var recommendedItems = new List<Item>();
        var failures = 0;

        if (gamesTask is not null)
        {
            if (gamesTask.Status == TaskStatus.RanToCompletion)
                recommendedItems.AddRange(gamesTask.Result);
            else
            {
                failures++;
                logger.LogWarning("RAWG discover failed: {Error}",
                    gamesTask.Exception?.GetBaseException().Message ?? "Unknown error");
            }
        }

        if (booksTask is not null)
        {
            if (booksTask.Status == TaskStatus.RanToCompletion)
                recommendedItems.AddRange(booksTask.Result);
            else
            {
                failures++;
                logger.LogWarning("OpenLibrary discover failed: {Error}",
                    booksTask.Exception?.GetBaseException().Message ?? "Unknown error");
            }
        }

        if (failures == tasks.Count)
            throw new InvalidOperationException("All sources failed to generate recommendations.");

        var ranked = ranker.Rank(
            recommendedItems,
            userSeedsFromDb,
            new RankingDTOs.RankingOptions(
                SimilarityWeight: 0.65,
                PopularityWeight: 0.10,
                RecencyWeight:    0.05,
                NoveltyWeight:    0.20,
                UseDiversification: true,
                DiversificationK:  50 
            ),
            ct);


        var takeAmount = request.NumberOfRecommendations > 0 ? request.NumberOfRecommendations : 20;
        List<GenerateRecommendationsDTOs.GenerateRecommendationsItemResponse> top =
            ranked.Take(takeAmount).Select(s => (GenerateRecommendationsDTOs.GenerateRecommendationsItemResponse)s)
                .ToList();

        GenerateRecommendationsDTOs.GenerateRecommendationsResponse response =
            new GenerateRecommendationsDTOs.GenerateRecommendationsResponse(top);

        return response;

        // TODO: persistir recommendedItems no banco.
    }
}
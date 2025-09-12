using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopCultureMashup.Application.Abstractions;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.Settings;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.UseCases.Recommend;

public sealed class GenerateRecommendationsHandler(
    ISeedRepository repository,
    IItemRepository itemRepository,
    IRecommendationRepository recommendationRepository,
    IRawgClient rawgClient,
    IOpenLibraryClient openLibClient,
    ILogger<GenerateRecommendationsHandler> logger,
    IRecommendationRanker ranker,
    IOptions<RecommendationSettings> settings)
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

        Task<IReadOnlyList<Item>>? gamesTask =
            rawgClient.DiscoverGamesAsync(allGenres, allThemes, pageSize: request.NumberOfRecommendations, ct: ct);
        Task<IReadOnlyList<Item>>? booksTask =
            openLibClient.DiscoverBooksAsync(allThemes, allCreators, limit: request.NumberOfRecommendations, ct: ct);

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

        var cfg = settings.Value;
        var rankingOption = new RankingDTOs.RankingOptions(
            SimilarityWeight: cfg.SimilarityWeight,
            PopularityWeight: cfg.PopularityWeight,
            RecencyWeight: cfg.RecencyWeight,
            NoveltyWeight: cfg.NoveltyWeight,
            UseDiversification: cfg.UseDiversification,
            DiversificationK: cfg.DiversificationK
        );

        var ranked = ranker.Rank(
            recommendedItems,
            userSeedsFromDb,
            rankingOption, ct);


        var takeAmount = request.NumberOfRecommendations > 0 ? request.NumberOfRecommendations : 20;


        var topRanked = ranked.Take(takeAmount).ToList();


        var session = new Recommendation
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,


            TotalCandidates = recommendedItems.Count,
            TotalReturned = topRanked.Count,
            SimilarityW = (decimal?)rankingOption.SimilarityWeight,
            PopularityW = (decimal?)rankingOption.PopularityWeight,
            RecencyW = (decimal?)rankingOption.RecencyWeight,
            NoveltyW = (decimal?)rankingOption.NoveltyWeight,
        };

        var persisted = await itemRepository.UpsertRangeAsync(topRanked.Select(x => x.Item), ct);
        var idMap = persisted.ToDictionary(i => $"{i.Source}||{i.ExternalId}", i => i.Id,
            StringComparer.OrdinalIgnoreCase);

        int rankPos = 1;
        foreach (var s in topRanked)
        {
            var key = $"{s.Item.Source}||{s.Item.ExternalId}";
            session.Results.Add(new RecommendationResult
            {
                Id = Guid.NewGuid(),
                RecommendationId = session.Id,
                ItemId = idMap[key],
                Rank = rankPos++,
                Score = (decimal)s.Score,
                //TODO detailed scores for dynamic weights
                GenresScore = 0, ThemesScore = 0, YearScore = 0, PopularityScore = 0, TextScore = 0, FranchiseBonus = 0
            });
        }

        await recommendationRepository.SaveAsync(session, ct);

        var top =
            topRanked.Select(s => (GenerateRecommendationsDTOs.RecommendationsItem)s)
                .ToList();

        return new GenerateRecommendationsDTOs.GenerateRecommendationsResponse(top);
    }
}
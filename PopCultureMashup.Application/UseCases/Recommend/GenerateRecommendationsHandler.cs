using Microsoft.Extensions.Logging;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.UseCases.Recommend;

public sealed class GenerateRecommendationsHandler(
    ISeedRepository repository,
    IItemRepository itemRepository,
    IRawgClient rawgClient,
    IOpenLibraryClient openLibClient,
    ILogger<GenerateRecommendationsHandler> logger)
{
    public async Task HandleAsync(GenerateRecommendationsDTOs.GenerateRecommendationsRequest request,
        CancellationToken ct = default)
    {
        var userSeedsFromDb = await repository.GetByUserIdAsync(request.UserId, ct);
        if (!userSeedsFromDb.Any())
            throw new InvalidOperationException("No seeds found for the user.");

        var gameSeeds = userSeedsFromDb.Where(s => s.Item.Type == ItemType.Game).ToList();
        var bookSeeds = userSeedsFromDb.Where(s => s.Item.Type == ItemType.Book).ToList();

        Task<IReadOnlyList<Item>>? gamesTask = null;
        Task<IReadOnlyList<Item>>? booksTask = null;

        // games
        if (gameSeeds.Any())
        {
            var gameGenres = gameSeeds.SelectMany(s => s.Item.Genres).Select(g => g.Genre).Distinct();
            var gameTags   = gameSeeds.SelectMany(s => s.Item.Themes).Select(t => t.Theme).Distinct();
            gamesTask = rawgClient.DiscoverGamesAsync(gameGenres, gameTags, ct: ct);
        }

        // books
        if (bookSeeds.Any())
        {
            var subjects = bookSeeds.SelectMany(s => s.Item.Themes).Select(t => t.Theme).Distinct();
            var authors  = bookSeeds.SelectMany(s => s.Item.Creators).Select(c => c.CreatorName).Distinct();
            booksTask = openLibClient.DiscoverBooksAsync(subjects, authors, ct: ct);
        }

        var tasks = new List<Task<IReadOnlyList<Item>>>();
        if (gamesTask is not null) tasks.Add(gamesTask);
        if (booksTask is not null) tasks.Add(booksTask);

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
                logger.LogInformation("RAWG discover failed: {Error}",
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
                logger.LogInformation("OpenLibrary discover failed: {Error}",
                    booksTask.Exception?.GetBaseException().Message ?? "Unknown error");
            }
        }

        if (failures == tasks.Count)
            throw new InvalidOperationException("All sources failed to generate recommendations.");

        // TODO: persistir recommendedItems no banco, montar response, etc.
    }
}

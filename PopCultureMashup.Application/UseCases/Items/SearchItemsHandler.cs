using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace PopCultureMashup.Application.UseCases.Items;

/// <summary>
/// Handles search operations for games and books from multiple data sources in parallel
/// </summary>
public class SearchItemsHandler(
    IRawgClient rawgClient,
    IOpenLibraryClient openLibraryClient,
    ILogger<SearchItemsHandler> logger) : ISearchItemsHandler
{
    /// <summary>
    /// Searches for games and books in parallel based on query term
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of search results from all sources</returns>
    public async Task<IEnumerable<SearchItemResponse>> SearchGamesAndBooksAsync(string query,
        CancellationToken ct = default)
    {
        logger.LogInformation("Searching for games and books with query: {Query}", query);

        // Start API requests in parallel
        var gamesTask = rawgClient.SearchGamesAsync(query, ct: ct);
        var booksTask = openLibraryClient.SearchBooksAsync(query, ct: ct);

        var tasks = new List<Task<IReadOnlyList<Item>>> { gamesTask, booksTask };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Exceptions are handled below per task
        }

        IReadOnlyList<Item> games = Array.Empty<Item>();
        IReadOnlyList<Item> books = Array.Empty<Item>();
        var failures = 0;

        if (gamesTask.Status == TaskStatus.RanToCompletion)
        {
            games = gamesTask.Result;
        }
        else
        {
            failures++;
            logger.LogWarning("RAWG search failed: {Error}",
                gamesTask.Exception?.GetBaseException().Message ?? "Unknown error");
        }

        if (booksTask.Status == TaskStatus.RanToCompletion)
        {
            books = booksTask.Result;
        }
        else
        {
            failures++;
            logger.LogWarning("OpenLibrary search failed: {Error}",
                booksTask.Exception?.GetBaseException().Message ?? "Unknown error");
        }

        if (failures == tasks.Count)
        {
            throw new InvalidOperationException("All search sources failed.");
        }

        // Map results to DTOs
        var gameDtos = games.Select(g => new SearchItemDto(
            Id: Guid.NewGuid(),
            Title: g.Title,
            Type: nameof(ItemType.Game),
            Description: g.Summary,
            ExternalId: g.ExternalId
        ));

        var bookDtos = books.Select(b => new SearchItemDto(
            Id: Guid.NewGuid(),
            Title: b.Title,
            Type: nameof(ItemType.Book),
            Description: b.Summary,
            ExternalId: b.ExternalId
        ));

        // Return combined results
        return new List<SearchItemResponse>
        {
            new SearchItemResponse(gameDtos),
            new SearchItemResponse(bookDtos)
        };
    }
}
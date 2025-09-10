using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.UseCases.Items;

public class SearchItemHandler(IRawgClient rawgClient, IOpenLibraryClient openLibraryClient) : ISearchItemsHandler
{
    public async Task<IEnumerable<SearchItemResponse>> SearchGamesAndBooksAsync(string query, CancellationToken ct = default)
    {
        var gamesTask = rawgClient.SearchGamesAsync(query, ct: ct);
        var booksTask = openLibraryClient.SearchBooksAsync(query, ct: ct);

        await Task.WhenAll(gamesTask, booksTask);

        var games = await gamesTask;
        var books = await booksTask;

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

        return new List<SearchItemResponse>
        {
            new SearchItemResponse(gameDtos),
            new SearchItemResponse(bookDtos)
        };
    }
}
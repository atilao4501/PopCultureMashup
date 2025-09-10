using PopCultureMashup.Application.DTOs;

namespace PopCultureMashup.Application.UseCases.Items;

public interface ISearchItemsHandler
{
    Task<IEnumerable<SearchItemResponse>> SearchGamesAndBooksAsync(string query, CancellationToken ct = default);
}
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface IRawgClient
{
    Task<Item?> FetchGameAsync(string externalId, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> SearchGamesAsync(string query, int pageSize = 10, CancellationToken ct = default);
}
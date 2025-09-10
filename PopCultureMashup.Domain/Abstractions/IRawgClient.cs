using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface IRawgClient
{
    Task<Item?> FetchGameAsyncByExternalID(string externalId, CancellationToken ct = default);

    Task<IReadOnlyList<Item>> DiscoverGamesAsync(
        IEnumerable<string> genres,
        IEnumerable<string> tags,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<IReadOnlyList<Item>> SearchGamesAsync(string query, int pageSize = 10, CancellationToken ct = default);
}
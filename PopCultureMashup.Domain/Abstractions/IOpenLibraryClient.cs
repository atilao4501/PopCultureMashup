using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface IOpenLibraryClient
{
    Task<Item?> FetchBookAsync(string workId, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> SearchBooksAsync(string query, int limit = 10, CancellationToken ct = default);
}
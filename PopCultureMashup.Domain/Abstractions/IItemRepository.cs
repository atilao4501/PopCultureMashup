using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface IItemRepository
{
    Task<Item?> GetBySourceIdAsync(string source, string externalId, CancellationToken ct = default);

    Task<Item> UpsertAsync(Item item,
        CancellationToken ct = default);
}
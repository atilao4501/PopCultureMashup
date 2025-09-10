using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface ISeedRepository
{
    Task AddRangeAsync(IEnumerable<Seed> seeds, CancellationToken ct = default);
    Task<IReadOnlyList<Seed>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
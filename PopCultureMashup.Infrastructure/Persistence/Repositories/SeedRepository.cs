using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Infrastructure.Persistence.Repositories;

public class SeedRepository(AppDbContext db) : ISeedRepository
{
    public async Task AddRangeAsync(IEnumerable<Seed> seeds, CancellationToken ct = default)
    {
        await db.Seeds.AddRangeAsync(seeds, ct);
        await db.SaveChangesAsync(ct);
    }

    //READONLY BECAUSE OF NO TRACKING
    public async Task<IReadOnlyList<Seed>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.Seeds
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }
}
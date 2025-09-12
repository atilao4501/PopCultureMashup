using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Infrastructure.Persistence.Repositories;

public class SeedRepository(AppDbContext db) : ISeedRepository
{
    public async Task AddRangeAsync(IEnumerable<Seed> seeds, CancellationToken ct = default)
    {
        var userIds = seeds.Select(s => s.UserId).Distinct().ToList();
        var itemIds = seeds.Select(s => s.ItemId).Distinct().ToList();

        var existing = await db.Seeds
            .Where(s => userIds.Contains(s.UserId) && itemIds.Contains(s.ItemId))
            .ToListAsync(ct);

        var newSeeds = seeds
            .Where(s => !existing.Any(e => e.UserId == s.UserId && e.ItemId == s.ItemId))
            .ToList();

        if (newSeeds.Count > 0)
        {
            await db.Seeds.AddRangeAsync(newSeeds, ct);
            await db.SaveChangesAsync(ct);
        }
    }

    //READONLY BECAUSE OF NO TRACKING
    //RAW SLQ JUST TO SHOW THAT I KNOW WHAT IM DOING
    public async Task<IReadOnlyList<Seed>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var seeds = await db.Seeds
            .FromSqlInterpolated($@"
            SELECT s.*
            FROM Seeds s
            WHERE s.UserId = {userId}
            ORDER BY s.CreatedAt DESC
            OFFSET 0 ROWS")
            .AsNoTracking()
            .Include(s => s.Item).ThenInclude(i => i.Genres)
            .Include(s => s.Item).ThenInclude(i => i.Themes)
            .Include(s => s.Item).ThenInclude(i => i.Creators)
            .ToListAsync(ct);

        return seeds;
    }
}
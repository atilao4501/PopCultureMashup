using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Infrastructure.Persistence.Repositories;

public class ItemRepository(AppDbContext db) : IItemRepository
{
    // READ-ONLY
    public Task<Item?> GetBySourceIdAsync(string source, string externalId, CancellationToken ct = default)
        => db.Items
             .AsNoTracking()
             .FirstOrDefaultAsync(x => x.Source == source && x.ExternalId == externalId, ct);

    public async Task<Item> UpsertAsync(Item model, CancellationToken ct = default)
    {
        var genres  = (model.Genres  ?? new List<ItemGenre>()).Select(g => g.Genre)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

        var themes  = (model.Themes  ?? new List<ItemTheme>()).Select(t => t.Theme)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

        var creators = (model.Creators ?? new List<ItemCreator>()).Select(c => c.CreatorName)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

        // 2) Upsert the core Item (without joins)
        var existing = await db.Items
            .FirstOrDefaultAsync(x => x.Source == model.Source && x.ExternalId == model.ExternalId, ct);

        Guid itemId;

        if (existing is null)
        {
            // new item
            var core = new Item
            {
                Id         = Guid.NewGuid(),
                Type       = model.Type,
                Title      = model.Title,
                Year       = model.Year,
                Popularity = model.Popularity,
                Summary    = model.Summary,
                Source     = model.Source,
                ExternalId = model.ExternalId
            };

            db.Items.Add(core);
            await db.SaveChangesAsync(ct);

            itemId = core.Id;
            model  = core; // return the tracked entity with generated Id
        }
        else
        {
            // update core fields
            existing.Title      = model.Title;
            existing.Year       = model.Year;
            existing.Popularity = model.Popularity;
            existing.Summary    = model.Summary;

            await db.SaveChangesAsync(ct);

            itemId = existing.Id;
            model  = existing;
        }

        // 3) Reset joins (clear-and-insert)
        await db.ItemGenres.Where(x => x.ItemId == itemId).ExecuteDeleteAsync(ct);
        await db.ItemThemes.Where(x => x.ItemId == itemId).ExecuteDeleteAsync(ct);
        await db.ItemCreators.Where(x => x.ItemId == itemId).ExecuteDeleteAsync(ct);

        if (genres.Count > 0)
            db.ItemGenres.AddRange(genres.Select(g => new ItemGenre   { ItemId = itemId, Genre       = g }));

        if (themes.Count > 0)
            db.ItemThemes.AddRange(themes.Select(t => new ItemTheme   { ItemId = itemId, Theme       = t }));

        if (creators.Count > 0)
            db.ItemCreators.AddRange(creators.Select(c => new ItemCreator { ItemId = itemId, CreatorName = c }));

        await db.SaveChangesAsync(ct);

        return model; // with Id set
    }

    public async Task AddSeedsAsync(IEnumerable<Seed> seeds, CancellationToken ct = default)
    {
        await db.Seeds.AddRangeAsync(seeds, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<Seed>> GetRecentSeedsAsync(Guid userId, int take, CancellationToken ct = default)
        => db.Seeds.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Seed>)t.Result, ct);
}

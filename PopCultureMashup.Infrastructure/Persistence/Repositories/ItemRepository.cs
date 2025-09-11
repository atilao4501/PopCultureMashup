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

    public async Task<Item> UpsertAsync(Item item, CancellationToken ct = default)
    {
        var genres = (item.Genres ?? new List<ItemGenre>()).Select(g => g.Genre)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var themes = (item.Themes ?? new List<ItemTheme>())
            .Where(t => !string.IsNullOrWhiteSpace(t.Theme) && !string.IsNullOrWhiteSpace(t.Slug))
            .Select(t => new { Theme = t.Theme.Trim(), Slug = t.Slug.Trim() })
            .GroupBy(x => x.Theme, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var creators = (item.Creators ?? new List<ItemCreator>())
            .Where(c => !string.IsNullOrWhiteSpace(c.CreatorName) || !string.IsNullOrWhiteSpace(c.Slug))
            .Select(c => new { Name = c.CreatorName.Trim(), Slug = c.Slug?.Trim() })
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        // 2) Upsert the core Item (without joins)
        var existing = await db.Items
            .FirstOrDefaultAsync(x => x.Source == item.Source && x.ExternalId == item.ExternalId, ct);

        Guid itemId;

        if (existing is null)
        {
            // new item
            var core = new Item
            {
                Id = Guid.NewGuid(),
                Type = item.Type,
                Title = item.Title,
                Year = item.Year,
                Popularity = item.Popularity,
                Summary = item.Summary,
                Source = item.Source,
                ExternalId = item.ExternalId
            };

            db.Items.Add(core);
            await db.SaveChangesAsync(ct);

            itemId = core.Id;
            item = core; // return the tracked entity with generated Id
        }
        else
        {
            // update core fields
            existing.Title = item.Title;
            existing.Year = item.Year;
            existing.Popularity = item.Popularity;
            existing.Summary = item.Summary;

            await db.SaveChangesAsync(ct);

            itemId = existing.Id;
            item = existing;
        }

        // 3) Reset joins (clear-and-insert)
        await db.ItemGenres.Where(x => x.ItemId == itemId).ExecuteDeleteAsync(ct);
        await db.ItemThemes.Where(x => x.ItemId == itemId).ExecuteDeleteAsync(ct);
        await db.ItemCreators.Where(x => x.ItemId == itemId).ExecuteDeleteAsync(ct);

        if (genres.Count > 0)
            db.ItemGenres.AddRange(genres.Select(g => new ItemGenre { ItemId = itemId, Genre = g }));

        if (themes.Count > 0)
            db.ItemThemes.AddRange(themes.Select(t => new ItemTheme
            {
                ItemId = itemId,
                Theme = t.Theme,
                Slug = t.Slug
            }));

        if (creators.Count > 0)
            db.ItemCreators.AddRange(creators.Select(c => new ItemCreator
            {
                ItemId = itemId,
                CreatorName = c.Name,
                Slug = c.Slug
            }));
        await db.SaveChangesAsync(ct);

        return item; // with Id set
    }

    public async Task<List<Item>> UpsertRangeAsync(IEnumerable<Item> incoming, CancellationToken ct = default)
    {
        var items = incoming
            .Where(i => !string.IsNullOrWhiteSpace(i.Source) && !string.IsNullOrWhiteSpace(i.ExternalId))
            .ToList();
        
        const string SEP = "||";
        string Key(string s, string e) => $"{s}{SEP}{e}";

        var keys = items.Select(i => Key(i.Source!, i.ExternalId!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        var existing = await db.Items
            .Where(x => keys.Contains(x.Source + SEP + x.ExternalId))
            .ToListAsync(ct);
        
        var byKey = existing.ToDictionary(
            e => Key(e.Source, e.ExternalId),
            e => e,
            StringComparer.OrdinalIgnoreCase);

        var toInsert = new List<Item>();
        var genresToAdd = new List<ItemGenre>();
        var themesToAdd = new List<ItemTheme>();
        var creatorsToAdd = new List<ItemCreator>();
        
        var updatedExistingIds = new HashSet<Guid>();

        foreach (var i in items)
        {
            var key = Key(i.Source!, i.ExternalId!);

            if (byKey.TryGetValue(key, out var found))
            {
                found.Title = i.Title;
                found.Year = i.Year;
                found.Popularity = i.Popularity;
                found.Summary = i.Summary;

                updatedExistingIds.Add(found.Id);

                if (i.Genres is { Count: > 0 })
                    genresToAdd.AddRange(i.Genres.Select(g => new ItemGenre
                    {
                        ItemId = found.Id,
                        Genre = g.Genre
                    }));

                if (i.Themes is { Count: > 0 })
                    themesToAdd.AddRange(i.Themes.Select(t => new ItemTheme
                    {
                        ItemId = found.Id,
                        Theme = t.Theme,
                        Slug = t.Slug ?? t.Theme
                    }));

                if (i.Creators is { Count: > 0 })
                    creatorsToAdd.AddRange(i.Creators.Select(c => new ItemCreator
                    {
                        ItemId = found.Id,
                        CreatorName = c.CreatorName,
                        Slug = c.Slug
                    }));
            }
            else
            {
                var core = new Item
                {
                    Id = Guid.NewGuid(),
                    Type = i.Type,
                    Title = i.Title,
                    Year = i.Year,
                    Popularity = i.Popularity,
                    Summary = i.Summary,
                    Source = i.Source!,
                    ExternalId = i.ExternalId!
                };

                toInsert.Add(core);
                byKey[key] = core;

                if (i.Genres is { Count: > 0 })
                    genresToAdd.AddRange(i.Genres.Select(g => new ItemGenre
                    {
                        ItemId = core.Id,
                        Genre = g.Genre
                    }));

                if (i.Themes is { Count: > 0 })
                    themesToAdd.AddRange(i.Themes.Select(t => new ItemTheme
                    {
                        ItemId = core.Id,
                        Theme = t.Theme,
                        Slug = t.Slug ?? t.Theme
                    }));

                if (i.Creators is { Count: > 0 })
                    creatorsToAdd.AddRange(i.Creators.Select(c => new ItemCreator
                    {
                        ItemId = core.Id,
                        CreatorName = c.CreatorName,
                        Slug = c.Slug
                    }));
            }
        }
        
        if (toInsert.Count > 0) db.Items.AddRange(toInsert);
        
        if (updatedExistingIds.Count > 0)
        {
            await db.ItemGenres
                .Where(x => updatedExistingIds.Contains(x.ItemId))
                .ExecuteDeleteAsync(ct);

            await db.ItemThemes
                .Where(x => updatedExistingIds.Contains(x.ItemId))
                .ExecuteDeleteAsync(ct);

            await db.ItemCreators
                .Where(x => updatedExistingIds.Contains(x.ItemId))
                .ExecuteDeleteAsync(ct);
        }
        
        if (genresToAdd.Count > 0) db.ItemGenres.AddRange(genresToAdd);
        if (themesToAdd.Count > 0) db.ItemThemes.AddRange(themesToAdd);
        if (creatorsToAdd.Count > 0) db.ItemCreators.AddRange(creatorsToAdd);

        await db.SaveChangesAsync(ct);

        return byKey.Values.ToList();
    }
}
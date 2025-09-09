using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

public sealed class SeedItemsHandler
{
    private readonly IItemRepository _items;
    private readonly ISeedRepository _seeds;
    private readonly IRawgClient _rawg;
    private readonly IOpenLibraryClient _openlib;

    public SeedItemsHandler(IItemRepository items, ISeedRepository seeds,
        IRawgClient rawg, IOpenLibraryClient openlib)
        => (_items, _seeds, _rawg, _openlib) = (items, seeds, rawg, openlib);

    public async Task<SeedResponse> HandleAsync(SeedRequest req, CancellationToken ct = default)
    {
        var upserted = 0;
        var newSeeds = new List<Seed>();

        foreach (var i in req.Items)
        {
            Item? item = i.Type.ToLowerInvariant() switch
            {
                "game" => await _rawg.FetchGameAsync(i.ExternalId, ct),
                "book" => await _openlib.FetchBookAsync(i.ExternalId, ct),
                _ => throw new ArgumentException($"Unknown type '{i.Type}'")
            };
            if (item is null) continue;

            var persisted = await _items.UpsertAsync(item, ct);
            upserted++;

            newSeeds.Add(new Seed
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                ItemId = persisted.Id, 
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newSeeds.Count > 0)
            await _seeds.AddRangeAsync(newSeeds, ct);

        return new SeedResponse(req.UserId, upserted, newSeeds.Count);
    }
}
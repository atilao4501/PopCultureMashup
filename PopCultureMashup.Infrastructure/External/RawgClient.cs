using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.External.DTOs;

namespace PopCultureMashup.Infrastructure.External;

public sealed class RawgClient : IRawgClient
{
    private readonly HttpClient _http;
    private readonly RawgOptions _opt;

    public RawgClient(HttpClient http, IOptions<RawgOptions> opt)
        => (_http, _opt) = (http, opt.Value);

    public async Task<Item?> FetchGameAsyncByExternalID(string externalId, CancellationToken ct = default)
    {
        var url = $"games/{externalId}?key={_opt.ApiKey}";
        var dto = await _http.GetFromJsonAsync<RawgGameDto>(url, ct);
        return dto is null ? null : Map(dto, externalId);
    }

    public async Task<IReadOnlyList<Item>> DiscoverGamesAsync(
        IEnumerable<string> genres,
        IEnumerable<string> tags,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var genresParam = genres is not null && genres.Any()
            ? $"&genres={string.Join(",", genres.Select(Uri.EscapeDataString))}"
            : string.Empty;

        var tagsParam = tags is not null && tags.Any()
            ? $"&tags={string.Join(",", tags.Select(Uri.EscapeDataString))}"
            : string.Empty;

        var url = $"games?page_size={pageSize}{genresParam}{tagsParam}&key={_opt.ApiKey}";

        var page = await _http.GetFromJsonAsync<RawgSearchPageDto>(url, ct);
        if (page?.results == null || page.results.Count == 0)
            return new List<Item>();

        return page.results
            .Select(g => Map(g, g.id.ToString()))
            .ToList();
    }

    public async Task<IReadOnlyList<Item>> SearchGamesAsync(
        string query, int pageSize = 10, CancellationToken ct = default)
    {
        var url = $"games?search={Uri.EscapeDataString(query)}&page_size={pageSize}&key={_opt.ApiKey}";
        var page = await _http.GetFromJsonAsync<RawgSearchPageDto>(url, ct);
        return page?.results?.Select(g => Map(g, g.id.ToString())).ToList() ?? new List<Item>();
    }
    
    private static Item Map(RawgGameDto g, string externalId)
        => new()
        {
            Type = ItemType.Game,
            Title = g.name,
            Year = TryYear(g.released),
            Popularity = g.rating,
            Summary = g.description_raw,
            Source = "rawg",
            ExternalId = externalId,
            
            Genres = (g.genres ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x?.slug))
                .Select(x => new ItemGenre { Genre = x.slug })
                .ToList(),
            
            Themes = (g.tags ?? new())
                .Where(t => !string.IsNullOrWhiteSpace(t?.slug))
                .Select(t => new ItemTheme { Theme = t.slug })
                .ToList(),
            
            Creators = (g.developers ?? new())
                .Where(d => !string.IsNullOrWhiteSpace(d?.name))
                .Select(d => new ItemCreator { CreatorName = d.name })
                .ToList()
        };

    private static int? TryYear(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : int.TryParse(s[..4], out var y) ? y : null;
}
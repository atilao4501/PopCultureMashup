using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.External.DTOs;

namespace PopCultureMashup.Infrastructure.External;

public sealed class RawgClient: IRawgClient
{
    private readonly HttpClient _http;
    private readonly RawgOptions _opt;

    public RawgClient(HttpClient http, IOptions<RawgOptions> opt)
        => (_http, _opt) = (http, opt.Value);

    public async Task<Item?> FetchGameAsync(string externalId, CancellationToken ct = default)
    {
        var url = $"games/{externalId}?key={_opt.ApiKey}";
        var dto = await _http.GetFromJsonAsync<RawgGameDto>(url, ct);
        return dto is null ? null : Map(dto, externalId);
    }

    public async Task<IReadOnlyList<Item>> SearchGamesAsync(string query, int pageSize = 10, CancellationToken ct = default)
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
            Genres = g.genres.Select(x => new ItemGenre { Genre = x.name }).ToList(),
            Creators = (g.developers ?? new()).Select(d => new ItemCreator { CreatorName = d.name }).ToList()
        };

    private static int? TryYear(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : int.TryParse(s[..4], out var y) ? y : null;

}
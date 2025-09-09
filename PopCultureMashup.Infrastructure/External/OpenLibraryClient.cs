using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.External.DTOs;

namespace PopCultureMashup.Infrastructure.External;

public sealed class OpenLibraryClient : IOpenLibraryClient
{
    private readonly HttpClient _http;
    private readonly OpenLibraryOptions _opt;

    public OpenLibraryClient(HttpClient http, IOptions<OpenLibraryOptions> opt)
        => (_http, _opt) = (http, opt.Value);

    /// <summary>
    /// Fetch a single Work by its Open Library workId (e.g., "OL82563W").
    /// GET /works/{workId}.json
    /// </summary>
    public async Task<Item?> FetchBookAsync(string workId, CancellationToken ct = default)
    {
        // OpenLibrary uses paths like "/works/OL82563W"
        var path = $"works/{workId}.json";
        using var resp = await _http.GetAsync(path, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<OpenLibWorkDto>(cancellationToken: ct);
        if (dto is null) return null;

        // Some descriptions come as string, others as { "value": "..." }
        var description = ParseDescription(dto.description);

        return new Item
        {
            Type = ItemType.Book,
            Title = dto.title,
            Year = TryYear(dto.first_publish_date),
            Popularity = null, // OpenLibrary doesn't provide a stable rating here
            Summary = description,
            Source = "openlibrary",
            ExternalId = workId, // store the bare id (no "/works/")
            // Themes from subjects
            Themes = (dto.subjects ?? new()).Take(12)
                .Select(s => new ItemTheme { Theme = s })
                .ToList(),
            // Creators aren't in the Work payload directly; we keep this empty here.
            // When using Search, we can map authors -> creators.
            Creators = new List<ItemCreator>()
        };
    }

    /// <summary>
    /// Full-text search for works.
    /// </summary>
    public async Task<IReadOnlyList<Item>> SearchBooksAsync(string query, int limit = 10,
        CancellationToken ct = default)
    {
        var path = $"search.json?q={Uri.EscapeDataString(query)}&limit={limit}";
        var page = await _http.GetFromJsonAsync<OpenLibSearchPageDto>(path, ct);
        if (page?.docs is null || page.docs.Count == 0)
            return Array.Empty<Item>();

        var items = new List<Item>(page.docs.Count);
        foreach (var d in page.docs)
        {
            var workId = ExtractWorkId(d.key); // "/works/OL82563W" -> "OL82563W"

            items.Add(new Item
            {
                Type = ItemType.Book,
                Title = d.title,
                Year = d.first_publish_year,
                Popularity = null,
                Summary = null,
                Source = "openlibrary",
                ExternalId = workId,
                Themes = (d.subject ?? new()).Take(8)
                    .Select(s => new ItemTheme { Theme = s })
                    .ToList(),
                Creators = (d.author_name ?? new())
                    .Select(a => new ItemCreator { CreatorName = a })
                    .ToList()
            });
        }

        return items;
    }

    private static int? TryYear(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var len = Math.Min(4, s.Length);
        return int.TryParse(s.AsSpan(0, len), out var year) ? year : null;
    }

    private static string? ParseDescription(object? description)
        => description switch
        {
            null => null,
            string s => s,
            // Sometimes description is { "value": "..." }
            System.Text.Json.Nodes.JsonObject o when o.TryGetPropertyValue("value", out var v) => v?.ToString(),
            _ => description.ToString()
        };

    private static string ExtractWorkId(string key)
        => key.Replace("/works/", "").Trim();
}
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.External.DTOs;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
        var path = $"works/{workId}.json";
        using var resp = await _http.GetAsync(path, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<OpenLibWorkDto>(cancellationToken: ct);
        if (dto is null) return null;

        var description = ParseDescription(dto.description);

        return new Item
        {
            Type = ItemType.Book,
            Title = dto.title,
            Year = TryYear(dto.first_publish_date),
            Popularity = null,
            Summary = description,
            Source = "openlibrary",
            ExternalId = workId, // "OL82563W"

            Themes = (dto.subjects ?? new())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .Select(s => new ItemTheme
                {
                    Theme = Trunc(s, 120),
                    Slug = SafeSlug(s, 120)
                })
                .ToList(),

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

                Themes = (d.subject ?? new())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .Select(s => new ItemTheme
                    {
                        Theme = Trunc(s, 120),
                        Slug = SafeSlug(s, 120)
                    })
                    .ToList(),

                Creators = (d.author_name ?? new())
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(a => new ItemCreator
                    {
                        CreatorName = Trunc(a, 160),
                        Slug = Slugify(a)
                    })
                    .ToList()
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<Item>> DiscoverBooksAsync(
        IEnumerable<string>? subjects,
        IEnumerable<string>? authors,
        int page = 1,
        int limit = 20,
        CancellationToken ct = default)
    {
        var hasSubjects = subjects is not null && subjects.Any();
        var hasAuthors = authors is not null && authors.Any();
        if (!hasSubjects && !hasAuthors)
            return Array.Empty<Item>();


        var qParts = new List<string>();

        if (hasSubjects)
        {
            qParts.AddRange(
                subjects!
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => $"subject:\"{s.Trim()}\""));
        }

        if (hasAuthors)
        {
            qParts.AddRange(
                authors!
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => $"author:\"{a.Trim()}\""));
        }

        var q = string.Join(" AND ", qParts);
        var url = $"search.json?q={Uri.EscapeDataString(q)}&page={page}&limit={limit}";

        var pageDto = await _http.GetFromJsonAsync<OpenLibSearchPageDto>(url, ct);
        if (pageDto?.docs is null || pageDto.docs.Count == 0)
            return Array.Empty<Item>();

        var items = new List<Item>(pageDto.docs.Count);
        foreach (var d in pageDto.docs)
        {
            var workId = ExtractWorkId(d.key);

            items.Add(new Item
            {
                Type = ItemType.Book,
                Title = d.title,
                Year = d.first_publish_year,
                Popularity = null,
                Summary = null,
                Source = "openlibrary",
                ExternalId = workId,

                Themes = (d.subject ?? new())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(12)
                    .Select(s => new ItemTheme
                    {
                        Theme = Trunc(s, 120),
                        Slug = SafeSlug(s, 120)
                    })
                    .ToList(),

                Creators = (d.author_name ?? new())
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(a => new ItemCreator
                    {
                        CreatorName = Trunc(a, 160),
                        Slug = Slugify(a)
                    })
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
            System.Text.Json.Nodes.JsonObject o when o.TryGetPropertyValue("value", out var v) => v?.ToString(),
            _ => description.ToString()
        };

    private static string ExtractWorkId(string key)
        => key.Replace("/works/", "").Trim();

    private static string Trunc(string s, int max)
        => s.Length <= max ? s : s[..max];

    private static string SafeSlug(string s, int max)
    {
        var slug = Slugify(s);
        if (string.IsNullOrWhiteSpace(slug))
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(s));
            slug = Convert.ToHexString(bytes).ToLowerInvariant();
        }

        return Trunc(slug, max);
    }

    private static string Slugify(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Trim();

        var norm = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in norm)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }

        s = sb.ToString().Normalize(NormalizationForm.FormC);

        s = s.ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\-]", "");
        s = Regex.Replace(s, @"-+", "-").Trim('-');
        return s;
    }
}
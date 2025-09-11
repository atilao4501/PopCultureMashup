using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;
using PopCultureMashup.Infrastructure.Config;
using PopCultureMashup.Infrastructure.External.DTOs;
using System.Globalization;
using System.Text;
using System.Text.Json;
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

        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        // Handle OpenLibrary redirects
        if (root.TryGetProperty("type", out var typeProp) &&
            typeProp.ValueKind == JsonValueKind.Object &&
            typeProp.TryGetProperty("key", out var keyProp) &&
            keyProp.GetString() == "/type/redirect" &&
            root.TryGetProperty("location", out var locProp))
        {
            var redirectId = locProp.GetString()?.Split('/').Last();
            if (!string.IsNullOrWhiteSpace(redirectId) && redirectId != workId)
                return await FetchBookAsync(redirectId!, ct);

            return null;
        }

        var dto = root.Deserialize<OpenLibWorkDto>();
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
            ExternalId = workId,

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
    /// Full-text search for works, with payload optimization and fallback when limit is default.
    /// </summary>
    public async Task<IReadOnlyList<Item>> SearchBooksAsync(
        string query,
        int limit = 10,
        CancellationToken ct = default)
    {
        const int DefaultLimit = 10;
        const int FallbackLimit = 5;

        var url = $"search.json?q={Uri.EscapeDataString(query)}&limit={limit}&fields={BuildFields(full: true)}";
        OpenLibSearchPageDto? page = null;

        try
        {
            page = await GetSearchPageAsync(url, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && limit == DefaultLimit)
        {
            // timeout/cancel local: tenta fallback
        }
        catch (HttpRequestException) when (limit == DefaultLimit)
        {
            // erro transitório: tenta fallback
        }

        if (page?.docs is { Count: > 0 })
            return MapDocsToItems(page.docs, fullFields: true);

        // Fallback apenas quando chamador não especifica limite (usa o default)
        if (limit == DefaultLimit)
        {
            using var shortCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            shortCts.CancelAfter(TimeSpan.FromSeconds(3));

            var fbUrl =
                $"search.json?q={Uri.EscapeDataString(query)}&limit={FallbackLimit}&fields={BuildFields(full: false)}";
            var fbPage = await GetSearchPageAsync(fbUrl, shortCts.Token);

            if (fbPage?.docs is { Count: > 0 })
                return MapDocsToItems(fbPage.docs, fullFields: false);
        }

        return Array.Empty<Item>();
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
            qParts.AddRange(subjects!.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => $"subject:\"{s.Trim()}\""));
        if (hasAuthors)
            qParts.AddRange(authors!.Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => $"author:\"{a.Trim()}\""));

        var q = string.Join(" OR ", qParts);
        var url = $"search.json?q={Uri.EscapeDataString(q)}&page={page}&limit={limit}&fields={BuildFields(full: true)}";

        OpenLibSearchPageDto? pageDto = null;
        try
        {
            pageDto = await GetSearchPageAsync(url, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // tenta fallback rápido
        }
        catch (HttpRequestException)
        {
            // tenta fallback rápido
        }

        if (pageDto?.docs is { Count: > 0 })
            return MapDocsToItems(pageDto.docs, fullFields: true);

        // Fallback rápido com menos payload (não muda comportamento, só ajuda na latência)
        using var shortCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        shortCts.CancelAfter(TimeSpan.FromSeconds(3));

        var fbUrl =
            $"search.json?q={Uri.EscapeDataString(q)}&page={page}&limit={Math.Min(limit, 5)}&fields={BuildFields(full: false)}";
        var fbPage = await GetSearchPageAsync(fbUrl, shortCts.Token);

        if (fbPage?.docs is { Count: > 0 })
            return MapDocsToItems(fbPage.docs, fullFields: false);

        return Array.Empty<Item>();
    }

    // ---- HTTP helpers (streaming + status handling) ----

    private async Task<OpenLibSearchPageDto?> GetSearchPageAsync(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        // Para 5xx/429/408 devolvemos null (deixa o chamador acionar fallback)
        if ((int)resp.StatusCode is >= 500 or 429 or 408)
            return null;

        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<OpenLibSearchPageDto>(stream, cancellationToken: ct);
    }

    private static string BuildFields(bool full) =>
        full
            ? "key,title,first_publish_year,subject,author_name"
            : "key,title,first_publish_year,author_name";

    // ---- Mapping ----

    private IReadOnlyList<Item> MapDocsToItems(List<OpenLibSearchPageDto.Doc> docs, bool fullFields)
    {
        var items = new List<Item>(docs.Count);

        foreach (var d in docs)
        {
            var workId = ExtractWorkId(d.key ?? "");
            if (string.IsNullOrWhiteSpace(workId)) continue;

            var creators = (d.author_name ?? new())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(a => new ItemCreator
                {
                    CreatorName = Trunc(a, 160),
                    Slug = Slugify(a)
                })
                .ToList();

            var themes = fullFields
                ? (d.subject ?? new())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .Select(s => new ItemTheme { Theme = Trunc(s, 120), Slug = SafeSlug(s, 120) })
                .ToList()
                : new List<ItemTheme>();

            items.Add(new Item
            {
                Type = ItemType.Book,
                Title = d.title,
                Year = d.first_publish_year,
                Popularity = null,
                Summary = null,
                Source = "openlibrary",
                ExternalId = workId,
                Themes = themes,
                Creators = creators
            });
        }

        return items;
    }

    // ---- Helpers ----

    private static int? TryYear(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var len = Math.Min(4, s.Length);
        return int.TryParse(s.AsSpan(0, len), out var year) ? year : null;
    }

    private static string? ParseDescription(object? description) =>
        description switch
        {
            null => null,
            string s => s,

            JsonElement el => el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Object when el.TryGetProperty("value", out var v) &&
                                          v.ValueKind == JsonValueKind.String
                    => v.GetString(),
                JsonValueKind.Null => null,
                _ => el.ToString()
            },

            OpenLibWorkDto.DescriptionUnion du => du.value,

            _ => description.ToString()
        };

    private static string ExtractWorkId(string key) => key.Replace("/works/", "").Trim();

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max];

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
// Application/Services/RecommendationRanker.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.Abstractions;
using PopCultureMashup.Application.Settings;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.Services
{
    public sealed class RecommendationRanker : IRecommendationRanker
    {
        // configuration settings
        private readonly RecommendationSettings _cfg;

        // constructor
        public RecommendationRanker(IOptions<RecommendationSettings> options)
        {
            _cfg = options.Value;
        }

        // Soft clamp to avoid exactly 0.0 or 1.0 after normalization
        private static double SoftClamp01(double v) => Math.Min(0.999, Math.Max(0.001, v));

        public IReadOnlyList<ScoredItemDTOs.ScoredItem> Rank(
            IEnumerable<Item> candidates,
            IEnumerable<Seed> seeds,
            RankingDTOs.RankingOptions? options = null,
            System.Threading.CancellationToken ct = default)
        {
            options ??= new RankingDTOs.RankingOptions();

            // ---- Seed signals ----
            var seedGenres = seeds
                .SelectMany(s => s.Item.Genres)
                .Select(g => g.Genre)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var seedThemeFreq = seeds
                .SelectMany(s => s.Item.Themes)
                .Select(t => t.Slug ?? t.Theme)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .GroupBy(s => s!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var seedCreators = seeds
                .SelectMany(s => s.Item.Creators)
                .Select(c => c.Slug ?? c.CreatorName)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var seedExternalIds = seeds
                .Select(s => s.Item.ExternalId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // ---- Dedup (robust for items without ExternalId) ----
            var uniqueCandidates = candidates
                .Where(i => i.ExternalId is null || !seedExternalIds.Contains(i.ExternalId))
                .GroupBy(i =>
                {
                    var src = i.Source?.Trim() ?? string.Empty;
                    var eid = i.ExternalId?.Trim();
                    if (!string.IsNullOrWhiteSpace(eid))
                        return $"SRC:{src}|EID:{eid}";

                    var title = (i.Title ?? string.Empty).Trim().ToLowerInvariant();
                    var year = i.Year?.ToString() ?? "?";
                    var type = i.Type.ToString();
                    var firstCreator = (i.Creators.FirstOrDefault()?.Slug
                                        ?? i.Creators.FirstOrDefault()?.CreatorName
                                        ?? string.Empty).Trim().ToLowerInvariant();
                    var firstTheme = (i.Themes.FirstOrDefault()?.Slug
                                      ?? i.Themes.FirstOrDefault()?.Theme
                                      ?? string.Empty).Trim().ToLowerInvariant();
                    return $"SRC:{src}|TITLE:{title}|YEAR:{year}|TYPE:{type}|CR:{firstCreator}|TH:{firstTheme}";
                })
                .Select(g => g.First())
                .ToList();

            int currentYear = DateTime.UtcNow.Year;

            // ---- Helpers ----
            static double JaccardIndex(ISet<string> a, ISet<string> b)
            {
                if (a.Count == 0 && b.Count == 0) return 0;
                var inter = a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count();
                var uni = a.Union(b, StringComparer.OrdinalIgnoreCase).Count();
                return uni == 0 ? 0 : (double)inter / uni;
            }

            static double WeightedJaccardIndex(IDictionary<string, int> a, IDictionary<string, int> b)
            {
                if (a.Count == 0 && b.Count == 0) return 0;
                var keys = a.Keys.Intersect(b.Keys, StringComparer.OrdinalIgnoreCase);
                double inter = keys.Sum(k => Math.Min(a[k], b[k]));
                double sumA = a.Sum(kv => kv.Value);
                double sumB = b.Sum(kv => kv.Value);
                double uni = sumA + sumB - inter;
                return uni <= 0 ? 0 : inter / uni;
            }

            static (double min, double max) GetMinMax(IEnumerable<double> xs)
            {
                var vals = xs.ToList();
                if (vals.Count == 0) return (0, 1);
                var min = vals.Min();
                var max = vals.Max();
                if (Math.Abs(max - min) < 1e-9) max = min + 1e-9;
                return (min, max);
            }

            static double Normalize(double v, (double min, double max) mm) => (v - mm.min) / (mm.max - mm.min);
            static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);

            static HashSet<string> BuildTerms(Item i) =>
                i.Genres.Select(x => x.Genre)
                    .Concat(i.Themes.Select(x => x.Slug ?? x.Theme))
                    .Concat(i.Creators.Select(x => x.Slug ?? x.CreatorName))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.ToLowerInvariant())
                    .ToHashSet();

            // ---- Raw feature extraction ----
            var raw = uniqueCandidates.Select(i =>
            {
                ct.ThrowIfCancellationRequested();

                var genres = i.Genres.Select(x => x.Genre)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var themeFreq = i.Themes.Select(x => x.Slug ?? x.Theme)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .GroupBy(s => s!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(gp => gp.Key, gp => gp.Count(), StringComparer.OrdinalIgnoreCase);

                var creators = i.Creators.Select(x => x.Slug ?? x.CreatorName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Type-aware similarity weights
                bool isBook = i.Type == ItemType.Book;
                double themeW = isBook ? _cfg.ThemeWeightBooks : _cfg.ThemeWeightDefault;
                double genreW = isBook ? _cfg.GenreWeightBooks : _cfg.GenreWeightDefault;
                double creatorW = isBook ? _cfg.CreatorWeightBooks : _cfg.CreatorWeightDefault;

                double simThemes = WeightedJaccardIndex(seedThemeFreq, themeFreq);
                double simGenres = JaccardIndex(genres, seedGenres);
                double simCreators = JaccardIndex(creators, seedCreators);
                double simComposite = simThemes * themeW + simGenres * genreW + simCreators * creatorW;

                double? popularity = i.Popularity;

                int ageYears = (i.Year.HasValue && i.Year.Value > 0)
                    ? Math.Max(0, currentYear - i.Year.Value)
                    : 50;

                // Softer recency for books
                double halfLife = isBook ? _cfg.HalfLifeBooks : _cfg.HalfLifeGames;
                double k = isBook ? 0.6 : 1.0;
                double recency = Math.Exp(-(k * ageYears / Math.Max(1.0, halfLife)));

                return new
                {
                    Item = i,
                    SimComposite = simComposite,
                    Popularity = popularity,
                    Recency = recency,
                    Novelty = 1.0, // placeholder
                    Terms = BuildTerms(i)
                };
            }).ToList();

            // ---- Per-type normalization (popularity, recency, similarity) ----
            var popularityByType = raw.GroupBy(x => x.Item.Type).ToDictionary(
                g => g.Key,
                g =>
                {
                    var vals = g.Select(x => x.Popularity).Where(v => v.HasValue).Select(v => v!.Value).ToList();
                    double median = vals.Count == 0 ? 0.5 : vals.OrderBy(v => v).ElementAt(vals.Count / 2);
                    var mm = GetMinMax(vals.Count == 0 ? new[] { median, median + 1e-9 } : vals);
                    return (mm, median);
                });

            var recencyByType = raw.GroupBy(x => x.Item.Type)
                .ToDictionary(g => g.Key, g => GetMinMax(g.Select(x => x.Recency)));

            var similarityByType = raw.GroupBy(x => x.Item.Type)
                .ToDictionary(g => g.Key, g => GetMinMax(g.Select(x => x.SimComposite)));

            var noveltyMM = GetMinMax(raw.Select(x => x.Novelty));

            // ---- Score computation ----
            var scoredRaw = raw.Select(x =>
            {
                var (popMM, popMedian) = popularityByType[x.Item.Type];

                // Popularity floor for books (helps classics with missing ratings)
                double basePopularity = x.Popularity ?? popMedian;
                if (x.Item.Type == ItemType.Book)
                    basePopularity = Math.Max(basePopularity, 0.35);

                double pop = Normalize(basePopularity, popMM);
                double rec = Normalize(x.Recency, recencyByType[x.Item.Type]);
                double sim = Normalize(x.SimComposite, similarityByType[x.Item.Type]);
                double nov = Normalize(x.Novelty, noveltyMM);

                double score =
                    sim * options.SimilarityWeight
                    + pop * options.PopularityWeight
                    + rec * options.RecencyWeight
                    + nov * options.NoveltyWeight;

                return new ScoredItemDTOs.ScoredItem(x.Item, score);
            }).ToList();

            // ---- Final per-type calibration (keeps intra-type ordering; improves cross-type comparability) ----
            var scoreMMByType = scoredRaw
                .GroupBy(s => s.Item.Type)
                .ToDictionary(g => g.Key, g => GetMinMax(g.Select(s => s.Score)));

            var scoredAll = scoredRaw
                .Select(s => new ScoredItemDTOs.ScoredItem(
                    s.Item,
                    SoftClamp01(Normalize(s.Score, scoreMMByType[s.Item.Type]))
                ))
                .OrderByDescending(x => x.Score)
                .ToList();

            if (!options.UseDiversification)
                return scoredAll;

            // ---- Diversification (MMR) + domain balancing ----
            const double lambda = 0.7;
            var pool = new List<ScoredItemDTOs.ScoredItem>(scoredAll);

            int availableGames = pool.Count(x => x.Item.Type == ItemType.Game);
            int availableBooks = pool.Count(x => x.Item.Type == ItemType.Book);
            if (availableGames == 0 || availableBooks == 0)
                return MmrOnly(pool, lambda);

            int seedGames = seeds.Count(s => s.Item.Type == ItemType.Game);
            int seedBooks = seeds.Count(s => s.Item.Type == ItemType.Book);
            double gameShareTarget = (seedGames + seedBooks) == 0 ? 0.5 : (double)seedGames / (seedGames + seedBooks);
            gameShareTarget = Clamp(gameShareTarget, 0.40, 0.60);

            int topK = Math.Min(options.DiversificationK, pool.Count);
            int maxGames = Math.Min((int)Math.Round(topK * gameShareTarget), availableGames);
            int maxBooks = Math.Min(topK - (int)Math.Round(topK * gameShareTarget), availableBooks);

            int deficit = topK - (maxGames + maxBooks);
            if (deficit > 0)
            {
                if (availableGames - maxGames >= availableBooks - maxBooks)
                    maxGames = Math.Min(availableGames, maxGames + deficit);
                else
                    maxBooks = Math.Min(availableBooks, maxBooks + deficit);
            }

            var termsMap = raw.ToDictionary(x => x.Item, x => x.Terms);
            var diversified = new List<ScoredItemDTOs.ScoredItem>(topK);

            double MmrScore(ScoredItemDTOs.ScoredItem candidate, List<ScoredItemDTOs.ScoredItem> selected)
            {
                if (selected.Count == 0) return candidate.Score;
                double maxSim = selected.Max(sel =>
                {
                    var a = termsMap[candidate.Item];
                    var b = termsMap[sel.Item];
                    if (a.Count == 0 && b.Count == 0) return 0;
                    var inter = a.Intersect(b).Count();
                    var uni = a.Union(b).Count();
                    return uni == 0 ? 0 : (double)inter / uni;
                });
                return lambda * candidate.Score + (1 - lambda) * (1 - maxSim);
            }

            int pickedGames = 0, pickedBooks = 0;
            bool pickGameNext;
            var bestGame = pool.Where(x => x.Item.Type == ItemType.Game).FirstOrDefault();
            var bestBook = pool.Where(x => x.Item.Type == ItemType.Book).FirstOrDefault();
            pickGameNext = bestGame != null && bestBook != null ? bestGame.Score >= bestBook.Score : true;

            while (diversified.Count < topK && pool.Count > 0)
            {
                IEnumerable<ScoredItemDTOs.ScoredItem> allowed;
                if (pickedGames < maxGames && pickedBooks < maxBooks)
                {
                    allowed = pickGameNext
                        ? pool.Where(c => c.Item.Type == ItemType.Game)
                        : pool.Where(c => c.Item.Type == ItemType.Book);
                }
                else if (pickedGames < maxGames)
                {
                    allowed = pool.Where(c => c.Item.Type == ItemType.Game);
                }
                else if (pickedBooks < maxBooks)
                {
                    allowed = pool.Where(c => c.Item.Type == ItemType.Book);
                }
                else
                {
                    allowed = pool;
                }

                var pickList = allowed as IList<ScoredItemDTOs.ScoredItem> ?? allowed.ToList();
                if (pickList.Count == 0)
                {
                    if (pickedGames < maxGames)
                        pickList = pool.Where(c => c.Item.Type == ItemType.Game).ToList();
                    else if (pickedBooks < maxBooks)
                        pickList = pool.Where(c => c.Item.Type == ItemType.Book).ToList();
                    else
                        pickList = pool;
                }

                var best = pickList.OrderByDescending(c => MmrScore(c, diversified)).First();

                diversified.Add(best);
                pool.Remove(best);

                if (best.Item.Type == ItemType.Game)
                {
                    pickedGames++;
                    pickGameNext = false;
                }
                else if (best.Item.Type == ItemType.Book)
                {
                    pickedBooks++;
                    pickGameNext = true;
                }
            }

            return diversified;

            // MMR diversification without quotas (single-domain fallback)
            static List<ScoredItemDTOs.ScoredItem> MmrOnly(List<ScoredItemDTOs.ScoredItem> poolLocal,
                double lambdaLocal)
            {
                if (poolLocal.Count == 0) return poolLocal;

                var diversifiedLocal = new List<ScoredItemDTOs.ScoredItem>(poolLocal.Count);
                var termsLocal = poolLocal
                    .Select(p => p.Item)
                    .Distinct()
                    .ToDictionary(i => i,
                        i => i.Genres.Select(x => x.Genre)
                            .Concat(i.Themes.Select(x => x.Slug ?? x.Theme))
                            .Concat(i.Creators.Select(x => x.Slug ?? x.CreatorName))
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s!.ToLowerInvariant())
                            .ToHashSet()
                    );

                double MmrLocal(ScoredItemDTOs.ScoredItem candidate)
                {
                    if (diversifiedLocal.Count == 0) return candidate.Score;
                    double maxSim = diversifiedLocal.Max(sel =>
                    {
                        var a = termsLocal[candidate.Item];
                        var b = termsLocal[sel.Item];
                        if (a.Count == 0 && b.Count == 0) return 0;
                        var inter = a.Intersect(b).Count();
                        var uni = a.Union(b).Count();
                        return uni == 0 ? 0 : (double)inter / uni;
                    });
                    return lambdaLocal * candidate.Score + (1 - lambdaLocal) * (1 - maxSim);
                }

                while (poolLocal.Count > 0)
                {
                    var best = poolLocal.OrderByDescending(MmrLocal).First();
                    diversifiedLocal.Add(best);
                    poolLocal.Remove(best);
                }

                return diversifiedLocal;
            }
        }
    }
}


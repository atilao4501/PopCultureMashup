using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.Abstractions;

public interface IRecommendationRanker
{
    IReadOnlyList<ScoredItemDTOs.ScoredItem> Rank(
        IEnumerable<Item> candidates,
        IEnumerable<Seed> seeds,
        RankingDTOs.RankingOptions? options = null,
        CancellationToken ct = default);
}
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface IRecommendationRepository
{
    Task<Recommendation> SaveAsync(Recommendation rec, CancellationToken ct = default);
}
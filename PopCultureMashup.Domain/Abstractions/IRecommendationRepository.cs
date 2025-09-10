using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface IRecommendationRepository
{
    Task<List<Recommendation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Recommendation> SaveAsync(Recommendation rec, CancellationToken ct = default);
}
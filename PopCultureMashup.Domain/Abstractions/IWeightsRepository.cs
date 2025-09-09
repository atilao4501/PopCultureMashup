using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface IWeightsRepository
{
    Task<Weight?> GetForUserAsync(Guid? userId, CancellationToken ct = default);
    Task SaveAsync(Weight weights, CancellationToken ct = default);
}
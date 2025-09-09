using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Infrastructure.Persistence.Repositories;

public class RecommendationRepository(AppDbContext db) : IRecommendationRepository
{
    public async Task<Recommendation> SaveAsync(Recommendation rec, CancellationToken ct = default)
    {
        await db.Recommendations.AddAsync(rec, ct);
        await db.SaveChangesAsync(ct);
        return rec;
    }

}
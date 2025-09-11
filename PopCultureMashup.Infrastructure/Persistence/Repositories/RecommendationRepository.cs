using Microsoft.EntityFrameworkCore;
using PopCultureMashup.Domain.Abstractions;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Infrastructure.Persistence.Repositories;

public class RecommendationRepository(AppDbContext db) : IRecommendationRepository
{
    public async Task<List<Recommendation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var userRecommendations =
            await db.Recommendations
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Include(r => r.Results)
                .ThenInclude(rc => rc.Item)
                .ToListAsync(ct);

        return userRecommendations;
    }

    public async Task<Recommendation> SaveAsync(Recommendation rec, CancellationToken ct = default)
    {
        await db.Recommendations.AddAsync(rec, ct);
        await db.SaveChangesAsync(ct);
        return rec;
    }
    
    public async Task AddRangeAsync(IEnumerable<Recommendation> recs, CancellationToken ct = default)
    {
        
        
        await db.Recommendations.AddRangeAsync(recs, ct);
        await db.SaveChangesAsync(ct);
    }
}
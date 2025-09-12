using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.UseCases.Seed;

public interface ISeedItemsHandler
{
    public Task<SeedResponse> HandleAsync(SeedRequest req, CancellationToken ct = default);
    
    public Task<List<Item>> FetchUserSeedsAsync(Guid userId, CancellationToken ct = default);
}
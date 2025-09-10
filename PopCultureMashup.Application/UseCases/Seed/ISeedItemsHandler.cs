using PopCultureMashup.Application.DTOs;

namespace PopCultureMashup.Application.UseCases.Seed;

public interface ISeedItemsHandler
{
    public Task<SeedResponse> HandleAsync(SeedRequest req, CancellationToken ct = default);
}
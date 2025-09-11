using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Domain.Abstractions;

namespace PopCultureMashup.Application.UseCases.Recommend;

public class GetRecommendationHandler(IRecommendationRepository recommendationRepository)
{
    public async Task<GetRecommendationDTOs.GetRecommendationResponse> GetRecommendation(
        GetRecommendationDTOs.GetRecommendationRequest request)
    {
        var recommendationsFromDb = await recommendationRepository.GetByUserIdAsync(request.UserId);

        if (!recommendationsFromDb.Any())
            throw new InvalidOperationException("No recommendations found for the user.");

        var result = recommendationsFromDb
            .SelectMany(r => r.Results)
            .Select(i => (GenerateRecommendationsDTOs.RecommendationsItem)i)
            .DistinctBy(ri => ri.Title)
            .OrderByDescending(ri => ri.Score)
            .ToList();

        return new GetRecommendationDTOs.GetRecommendationResponse(result);
    }
}
using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.DTOs;

public record GenerateRecommendationsDTOs
{
    public record GenerateRecommendationsRequest(Guid UserId, int NumberOfRecommendations);

    public record GenerateRecommendationsResponse(List<GenerateRecommendationsItemResponse> Recommendations);

    public record GenerateRecommendationsItemResponse(double Score, string Title, string ExternalId, string Type)
    {
        public static implicit operator GenerateRecommendationsItemResponse(ScoredItemDTOs.ScoredItem scored)
            => new(
                scored.Score,
                scored.Item.Title,
                scored.Item.ExternalId,
                scored.Item.Type.ToString()
            );
    }
}
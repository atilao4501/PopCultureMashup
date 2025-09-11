using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.DTOs;

public record GenerateRecommendationsDTOs
{
    public record GenerateRecommendationsRequest(Guid UserId, int NumberOfRecommendations);

    public record GenerateRecommendationsResponse(List<RecommendationsItem> Recommendations);

    public record RecommendationsItem(double Score, string Title, string ExternalId, string Type)
    {
        public static implicit operator RecommendationsItem(ScoredItemDTOs.ScoredItem scored)
            => new(
                scored.Score,
                scored.Item.Title,
                scored.Item.ExternalId,
                scored.Item.Type.ToString()
            );
        
        public static implicit operator RecommendationsItem(RecommendationResult result)
            => new((double)result.Score, result.Item.Title, result.Item.ExternalId, result.Item.Type.ToString());
    }
}
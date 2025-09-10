namespace PopCultureMashup.Application.DTOs;

public record GenerateRecommendationsDTOs
{
    public record GenerateRecommendationsRequest(Guid UserId, int NumberOfRecommendations);
    public record GenerateRecommendationResponse(List<string> RecommendedItemIds);

}
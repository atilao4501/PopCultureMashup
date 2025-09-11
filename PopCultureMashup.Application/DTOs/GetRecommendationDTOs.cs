namespace PopCultureMashup.Application.DTOs;

public class GetRecommendationDTOs
{
    public record GetRecommendationRequest(Guid UserId);
    public record GetRecommendationResponse(List<GenerateRecommendationsDTOs.RecommendationsItem> Recommendations);
}
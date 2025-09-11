using PopCultureMashup.Domain.Entities;

public class Recommendation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    //analytics data
    public int TotalCandidates { get; set; }    
    public int TotalReturned { get; set; }     
    public decimal? SimilarityW { get; set; }   
    public decimal? PopularityW { get; set; }
    public decimal? RecencyW { get; set; }
    public decimal? NoveltyW { get; set; }

    public ICollection<RecommendationResult> Results { get; set; } = new List<RecommendationResult>();
}
namespace PopCultureMashup.Application.DTOs;

/// <summary>
/// Configuration options for ranking recommended items.
/// These weights control how much influence each factor has in the final score.
/// </summary>
public class RankingDTOs
{
    /// <summary>
    /// Defines the weighting strategy used when ranking recommendation candidates.
    /// </summary>
    /// <param name="SimilarityWeight">
    /// Weight applied to the similarity score between the candidate and the user's seeds.  
    /// Higher values make recommendations closer in genres, themes, or creators to the user's history.
    /// </param>
    /// <param name="PopularityWeight">
    /// Weight applied to the popularity signal (e.g., ratings count, average rating, external popularity metrics).  
    /// Higher values favor widely known or highly rated items.
    /// </param>
    /// <param name="RecencyWeight">
    /// Weight applied to the recency signal (based on release/publish year).  
    /// Higher values favor newer or more recently released items.
    /// </param>
    /// <param name="NoveltyWeight">
    /// Weight applied to the novelty factor (penalizing items too similar to the seeds).  
    /// Higher values encourage introducing fresh or less obvious items into the list.
    /// </param>
    /// <param name="UseDiversification">
    /// Whether to apply diversification (e.g., Maximal Marginal Relevance).  
    /// When true, ensures the final list is not dominated by highly similar items.
    /// </param>
    /// <param name="DiversificationK">
    /// Maximum number of items to keep after diversification is applied.  
    /// Controls how many recommendations are selected in the diversification loop.
    /// </param>
    public sealed record RankingOptions(
        double SimilarityWeight = 0.50,
        double PopularityWeight = 0.25,
        double RecencyWeight = 0.15,
        double NoveltyWeight = 0.10,
        bool   UseDiversification = true,
        int    DiversificationK = 25
    );
}

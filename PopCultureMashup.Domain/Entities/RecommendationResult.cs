using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents a single recommended item within a recommendation.
    /// Stores the recommended item and its ranking.
    /// Holds detailed scoring breakdown (genre, theme, year, popularity, text similarity, franchise bonus).
    /// Enables transparency and debugging of the recommendation process.
    /// </summary>
    public class RecommendationResult
    {
        /// <summary>
        /// Unique identifier for the recommendation result.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Reference to the parent recommendation this result belongs to.
        /// </summary>
        public Guid RecommendationId { get; set; }
        
        /// <summary>
        /// Reference to the recommended item.
        /// </summary>
        public Guid ItemId { get; set; }
        
        public Item Item { get; set; }
        
        /// <summary>
        /// The ranking position of this item within its recommendation set.
        /// </summary>
        public int Rank { get; set; }
        
        /// <summary>
        /// The overall recommendation score for this item.
        /// </summary>
        public decimal Score { get; set; }
        
        /// <summary>
        /// The score component based on genre similarity.
        /// </summary>
        public decimal GenresScore { get; set; }
        
        /// <summary>
        /// The score component based on thematic similarity.
        /// </summary>
        public decimal ThemesScore { get; set; }
        
        /// <summary>
        /// The score component based on year/release date proximity.
        /// </summary>
        public decimal YearScore { get; set; }
        
        /// <summary>
        /// The score component based on popularity alignment.
        /// </summary>
        public decimal PopularityScore { get; set; }
        
        /// <summary>
        /// The score component based on textual similarity analysis.
        /// </summary>
        public decimal TextScore { get; set; }
        
        /// <summary>
        /// Additional score bonus for items in the same franchise.
        /// </summary>
        public decimal FranchiseBonus { get; set; }
    }
}

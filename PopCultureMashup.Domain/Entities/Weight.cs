using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents the weighting configuration used to score recommendations.
    /// Contains multipliers for genres, themes, year, popularity, text similarity, and franchise bonus.
    /// Can be global or user-specific.
    /// Enables personalization of the recommendation algorithm.
    /// </summary>
    public class Weight
    {
        /// <summary>
        /// Unique identifier for the weight configuration.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Reference to the user this weight configuration belongs to, or null for global weights.
        /// </summary>
        public Guid? UserId { get; set; }
        
        /// <summary>
        /// Multiplier for the importance of genre matching in recommendations.
        /// </summary>
        public double Genres { get; set; }
        
        /// <summary>
        /// Multiplier for the importance of theme matching in recommendations.
        /// </summary>
        public double Themes { get; set; }
        
        /// <summary>
        /// Multiplier for the importance of year/release date proximity in recommendations.
        /// </summary>
        public double Year { get; set; }
        
        /// <summary>
        /// Multiplier for the importance of popularity alignment in recommendations.
        /// </summary>
        public double Popularity { get; set; }
        
        /// <summary>
        /// Multiplier for the importance of textual similarity in recommendations.
        /// </summary>
        public double Text { get; set; }
        
        /// <summary>
        /// Multiplier for additional scoring for items in the same franchise.
        /// </summary>
        public double Franchise { get; set; }
        
        /// <summary>
        /// The date and time when this weight configuration was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}

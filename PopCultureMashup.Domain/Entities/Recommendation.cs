using System;
using System.Collections.Generic;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents a recommendation session generated for a user.
    /// Stores the crossover direction (books → games or vice versa).
    /// Holds metadata about when and for whom it was generated.
    /// Acts as a container for RecommendationResult.
    /// </summary>
    public class Recommendation
    {
        /// <summary>
        /// Unique identifier for the recommendation session.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Reference to the user for whom this recommendation was generated.
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// The direction of the crossover recommendation (books to games or games to books).
        /// </summary>
        public CrossoverDirection Direction { get; set; }
        
        /// <summary>
        /// The date and time when the recommendation was generated.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Collection of individual recommended items with their scores.
        /// </summary>
        public ICollection<RecommendationResult> Results { get; set; } = new List<RecommendationResult>();
    }
}

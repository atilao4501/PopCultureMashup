using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Captures user feedback on recommended items.
    /// Links a user, a recommendation, and a specific item.
    /// Stores a feedback value (e.g., like/dislike).
    /// Used to refine or evaluate the recommendation quality.
    /// </summary>
    public class Feedback
    {
        /// <summary>
        /// Unique identifier for the feedback entry.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Reference to the user who provided the feedback.
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// Reference to the recommendation this feedback is related to.
        /// </summary>
        public Guid RecommendationId { get; set; }
        
        /// <summary>
        /// Reference to the specific item being rated.
        /// </summary>
        public Guid ItemId { get; set; }
        
        /// <summary>
        /// The feedback value provided by the user (e.g., like/dislike).
        /// </summary>
        public short Value { get; set; }
        
        /// <summary>
        /// The date and time when the feedback was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}

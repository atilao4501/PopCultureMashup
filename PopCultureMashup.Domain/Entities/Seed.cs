using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents an item chosen by a user as a seed for recommendations.
    /// Connects a user to an item they liked.
    /// Acts as the input for generating crossovers.
    /// </summary>
    public class Seed
    {
        /// <summary>
        /// Unique identifier for the seed entry.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Reference to the user who selected this item as a seed.
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// Reference to the item selected as a seed for recommendations.
        /// </summary>
        public Guid ItemId { get; set; }
        
        /// <summary>
        /// The date and time when this item was selected as a seed.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}

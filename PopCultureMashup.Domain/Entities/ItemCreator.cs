using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents a creator (author, developer, studio) of an item.
    /// Allows storing multiple creators per item.
    /// Useful for franchise or author-based recommendations.
    /// </summary>
    public class ItemCreator
    {
        /// <summary>
        /// Reference to the item associated with this creator.
        /// </summary>
        public Guid ItemId { get; set; }
        
        /// <summary>
        /// The name of the creator (author, developer, studio).
        /// </summary>
        public string CreatorName { get; set; } = null!;
        
        public string? Slug { get; set; } // opcional
    }
}

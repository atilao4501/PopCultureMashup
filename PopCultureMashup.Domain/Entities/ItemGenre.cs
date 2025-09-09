using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents a genre associated with an item.
    /// Many-to-one relationship with Item.
    /// Enables filtering and scoring by genre overlap.
    /// </summary>
    public class ItemGenre
    {
        /// <summary>
        /// Reference to the item associated with this genre.
        /// </summary>
        public Guid ItemId { get; set; }
        
        /// <summary>
        /// The name of the genre.
        /// </summary>
        public string Genre { get; set; } = null!;
    }
}

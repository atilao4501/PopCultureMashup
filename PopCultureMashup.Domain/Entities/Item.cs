using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents a cultural item (book or game).
    /// Contains core metadata: type, title, year, popularity, summary.
    /// Identified by an external source (Source, ExternalId).
    /// Serves as the central entity in the system.
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Unique identifier for the item.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The type of item (Book or Game).
        /// </summary>
        public ItemType Type { get; set; }
        
        /// <summary>
        /// The title of the item.
        /// </summary>
        public string Title { get; set; } = null!;
        
        /// <summary>
        /// The year the item was published or released.
        /// </summary>
        public int? Year { get; set; }
        
        /// <summary>
        /// A numerical representation of the item's popularity.
        /// </summary>
        public double? Popularity { get; set; }
        
        /// <summary>
        /// A textual summary or description of the item.
        /// </summary>
        public string? Summary { get; set; }
        
        /// <summary>
        /// The external source system that provided this item data.
        /// </summary>
        public string Source { get; set; } = null!;
        
        /// <summary>
        /// The unique identifier for this item in the external source system.
        /// </summary>
        public string ExternalId { get; set; } = null!;
    }
}

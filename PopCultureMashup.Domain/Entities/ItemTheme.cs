using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents a theme or narrative motif of an item.
    /// Many-to-one relationship with Item.
    /// Supports thematic similarity in recommendations.
    /// </summary>
    public class ItemTheme
    {
        /// <summary>
        /// Reference to the item associated with this theme.
        /// </summary>
        public Guid ItemId { get; set; }
        
        /// <summary>
        /// The name of the theme or narrative motif.
        /// </summary>
        public string Theme { get; set; } = null!;
    }
}

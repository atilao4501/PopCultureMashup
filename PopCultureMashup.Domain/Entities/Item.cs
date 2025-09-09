using System;
using System.Collections.Generic;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents a cultural item (book or game). Core metadata comes from an external source
    /// and the item owns navigation collections for genres, themes and creators.
    /// </summary>
    public class Item
    {
        /// <summary>Unique identifier for the item.</summary>
        public Guid Id { get; set; }

        /// <summary>The type of item (Book or Game).</summary>
        public ItemType Type { get; set; }

        /// <summary>The title of the item.</summary>
        public string Title { get; set; } = null!;

        /// <summary>The year the item was published or released.</summary>
        public int? Year { get; set; }

        /// <summary>A numerical representation of the item's popularity (API dependent).</summary>
        public double? Popularity { get; set; }

        /// <summary>A textual summary or description of the item.</summary>
        public string? Summary { get; set; }

        /// <summary>The external source system that provided this item data (e.g., "rawg", "openlibrary").</summary>
        public string Source { get; set; } = null!;

        /// <summary>The unique identifier for this item in the external source system.</summary>
        public string ExternalId { get; set; } = null!;

        // 🔗 Navigation collections (match tables: ItemGenres, ItemThemes, ItemCreators)
        /// <summary>Genres associated with the item (e.g., Action, Fantasy).</summary>
        public ICollection<ItemGenre> Genres { get; set; } = new List<ItemGenre>();

        /// <summary>Themes or narrative motifs associated with the item (e.g., Magic, Post-apocalyptic).</summary>
        public ICollection<ItemTheme> Themes { get; set; } = new List<ItemTheme>();

        /// <summary>Creators of the item (authors, developers, studios).</summary>
        public ICollection<ItemCreator> Creators { get; set; } = new List<ItemCreator>();
    }
}

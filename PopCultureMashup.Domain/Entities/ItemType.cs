namespace PopCultureMashup.Domain.Entities;

/// <summary>
/// Enumerates the type of an item.
/// </summary>
public enum ItemType : byte
{
    Unknown = 0,
    /// <summary>
    /// Represents a movie item.
    /// </summary>
    Movie = 1,
    /// <summary>
    /// Represents a TV series item.
    /// </summary>
    Series = 2,
    /// <summary>
    /// Represents a game item.
    /// </summary>
    Game = 3,
    /// <summary>
    /// Represents a book item.
    /// </summary>
    Book = 4
}

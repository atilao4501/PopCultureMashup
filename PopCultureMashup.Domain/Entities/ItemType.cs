namespace PopCultureMashup.Domain.Entities;

/// <summary>
/// Enumerates the type of an item.
/// </summary>
public enum ItemType : byte
{
    /// <summary>
    /// Represents a game item.
    /// </summary>
    Game = 0,
    /// <summary>
    /// Represents a book item.
    /// </summary>
    Book = 1
}

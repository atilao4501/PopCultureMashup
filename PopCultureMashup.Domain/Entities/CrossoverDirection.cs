namespace PopCultureMashup.Domain.Entities;

/// <summary>
/// Represents the direction of a crossover recommendation.
/// </summary>
public enum CrossoverDirection : byte
{
    Unknown = 0,
    /// <summary>
    /// Find books related to games (Games to Books).
    /// </summary>
    Inbound = 1,
    /// <summary>
    /// Find games inspired by books (Books to Games).
    /// </summary>
    Outbound = 2
}

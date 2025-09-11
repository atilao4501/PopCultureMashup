using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.DTOs;

public class ScoredItemDTOs
{
    public sealed record ScoredItem(Item Item, double Score);
}
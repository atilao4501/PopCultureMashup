using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Application.DTOs;

public record SearchItemRequest(string Query);
public record SearchItemDto(Guid Id, string Title, string Type, string? Description, string ExternalId);
public record SearchItemResponse(IEnumerable<SearchItemDto> Items);
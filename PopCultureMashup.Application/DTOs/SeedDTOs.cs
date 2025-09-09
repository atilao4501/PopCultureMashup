namespace PopCultureMashup.Application.DTOs;

public record SeedItemInput(string Type, string ExternalId); 
public record SeedRequest(Guid UserId, List<SeedItemInput> Items);
public record SeedResponse(Guid UserId, int UpsertedItems, int CreatedSeeds);

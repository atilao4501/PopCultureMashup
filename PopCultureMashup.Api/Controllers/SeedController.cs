using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Seed;
using PopCultureMashup.Domain.Entities; // SeedRequest/SeedResponse

namespace PopCultureMashup.Api.Controllers;

/// <summary>
/// API endpoints for initializing and populating the system database with games and books
/// </summary>
[ApiController]
[Route("seed")]
[Produces("application/json")]
public class SeedController(ISeedItemsHandler handler) : ControllerBase
{
    /// <summary>
    /// Imports a batch of games and books into the database.
    /// </summary>
    /// <param name="items">Collection of items (games and books) to import.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Summary of the seeding operation with counts and errors.</returns>
    /// <response code="201">Items were imported and a result resource was created (Location header returned).</response>
    /// <response code="200">Items were imported successfully (no result resource to reference).</response>
    /// <response code="400">The request body is null/empty or contains invalid items.</response>
    /// <response code="401">The user is not authenticated or the token is malformed.</response>
    /// <response code="500">An unexpected error occurred during the seeding process.</response>
    [HttpPost("create")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SeedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SeedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<ActionResult<SeedResponse>> Post(
        [FromBody] List<SeedItemInput> items,
        CancellationToken ct)
    {
        // Auth: extract userId from claims
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idClaim, out var userId))
            return Unauthorized("User not valid or token malformed.");

        // Model validation
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (items.Count == 0)
            return BadRequest("Provide at least one item.");

        //basic per-item guardrails to fail fast
        if (items.Any(i => string.IsNullOrWhiteSpace(i.Type) || string.IsNullOrWhiteSpace(i.ExternalId)))
            return BadRequest("Each item must have a non-empty Type and ExternalId.");

        var request = new SeedRequest(userId, items);
        var result = await handler.HandleAsync(request, ct);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves all seed items previously imported by the authenticated user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of seed items associated with the current user.</returns>
    /// <response code="200">Seed items were successfully retrieved.</response>
    /// <response code="401">The user is not authenticated or the token is malformed.</response>
    /// <response code="404">No seed items were found for the user.</response>
    /// <response code="500">An unexpected error occurred while retrieving seed items.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Item>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<ActionResult<List<Item>>> Get(
        CancellationToken ct)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idClaim, out var userId))
            return Unauthorized("User not valid or token malformed.");

        var items = await handler.FetchUserSeedsAsync(userId, ct);

        if (items.Count == 0)
            return NotFound("No seed items found for this user.");

        return Ok(items);
    }
}
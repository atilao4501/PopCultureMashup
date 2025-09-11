using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Seed; // SeedRequest/SeedResponse

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
    /// Imports a batch of games and books into the system database
    /// </summary>
    /// <param name="body">Collection of items (games and books) to be imported into the system</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Information about the seed operation including success count and errors</returns>
    /// <response code="201">Items were successfully imported into the database</response>
    /// <response code="400">If the request body is null or contains no items to seed</response>
    /// <response code="500">If an unexpected error occurs during the seeding process</response>
    [HttpPost]
    [ProducesResponseType(typeof(SeedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SeedResponse>> Post(
        [FromBody] SeedRequest body,
        CancellationToken ct)
    {
        if (body is null || body.Items is null || body.Items.Count == 0)
            throw new ArgumentException("Provide at least one item.");

        var res = await handler.HandleAsync(body, ct);
        return Created(string.Empty, res);
    }
}
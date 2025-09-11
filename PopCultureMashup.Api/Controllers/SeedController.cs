using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Seed; // SeedRequest/SeedResponse

namespace PopCultureMashup.Api.Controllers;

/// <summary>
/// Controller responsible for initial seeding of games and books data
/// </summary>
[ApiController]
[Route("seed")]
[Produces("application/json")]
public class SeedController(ISeedItemsHandler handler) : ControllerBase
{
    /// <summary>
    /// Performs initial seeding of games and books in the system
    /// </summary>
    /// <param name="body">List of items to seed</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Seed result with statistics of processed items</returns>
    /// <response code="201">Seed completed successfully</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="500">Internal server error</response>
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
using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Seed; // SeedRequest/SeedResponse

namespace PopCultureMashup.Api.Controllers;

[ApiController]
[Route("seed")]
public class SeedController(ISeedItemsHandler handler) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SeedResponse>> Post(
        [FromBody] SeedRequest body,
        CancellationToken ct)
    {
        if (body is null || body.Items is null || body.Items.Count == 0)
            return BadRequest("Provide at least one item.");

        var res = await handler.HandleAsync(body, ct);
        return Created(string.Empty, res);
    }
}
using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;   // SeedRequest/SeedResponse

namespace PopCultureMashup.Api.Controllers;

[ApiController]
[Route("seed")]
public class SeedController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SeedResponse>> Post(
        [FromServices] SeedItemsHandler handler,
        [FromBody] SeedRequest body,
        CancellationToken ct)
    {
        if (body is null || body.Items is null || body.Items.Count == 0)
            return BadRequest("Provide at least one item.");

        var res = await handler.HandleAsync(body, ct);
        return Created(string.Empty, res);
    }
}
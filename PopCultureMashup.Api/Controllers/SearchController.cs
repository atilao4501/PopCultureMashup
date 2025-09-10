using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Items;

namespace PopCultureMashup.Api.Controllers;

[ApiController]
[Route("search")]
public class SearchController(ISearchItemsHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SearchItemResponse>> Get(
        [FromQuery] string query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty.");

        var res = await handler.SearchGamesAndBooksAsync(query, ct);
        
        return Ok(res);
    }
}
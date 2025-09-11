using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Items;

namespace PopCultureMashup.Api.Controllers;

/// <summary>
/// Controller responsible for searching games and books
/// </summary>
[ApiController]
[Route("search")]
[Produces("application/json")]
public class SearchController(ISearchItemsHandler handler) : ControllerBase
{
    /// <summary>
    /// Searches for games and books based on a search term
    /// </summary>
    /// <param name="query">Search term to find games and books</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of found games and books</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid or empty search term</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(SearchItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SearchItemResponse>> Get(
        [FromQuery] string query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty.");

        var res = await handler.SearchGamesAndBooksAsync(query, ct);
        
        return Ok(res);
    }
}
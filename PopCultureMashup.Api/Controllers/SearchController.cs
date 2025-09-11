using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Items;

namespace PopCultureMashup.Api.Controllers;

/// <summary>
/// API endpoints for searching games and books across multiple databases
/// </summary>
[ApiController]
[Route("search")]
[Produces("application/json")]
public class SearchController(ISearchItemsHandler handler) : ControllerBase
{
    /// <summary>
    /// Searches for games and books based on the provided query term
    /// </summary>
    /// <param name="query">Text to search for matching games and books (title, description, etc.)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A collection of games and books matching the search criteria</returns>
    /// <response code="200">Returns the list of items that match the search query</response>
    /// <response code="400">If the search query is null, empty or invalid</response>
    /// <response code="500">If an unexpected error occurs during the search process</response>
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
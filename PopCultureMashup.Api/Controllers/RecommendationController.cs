using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Recommend;

namespace PopCultureMashup.Api.Controllers;

/// <summary>
/// API endpoints for generating and retrieving personalized recommendations for games and books
/// </summary>
[ApiController]
[Route("/recommendations")]
[Produces("application/json")]
public class RecommendationController(
    GenerateRecommendationsHandler generateRecommendationHandler,
    GetRecommendationHandler getRecommendationHandler) : ControllerBase
{
    /// <summary>
    /// Generates personalized recommendations based on user preferences and history.
    /// Requires authentication (JWT).
    /// </summary>
    /// <param name="numberOfRecommendations">Number of recommendations to return (minimum 1)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of recommended items (games and books) with relevance scores</returns>
    /// <response code="200">Recommendations were successfully generated based on user profile</response>
    /// <response code="400">If the query parameter is invalid</response>
    /// <response code="401">If the user is not authenticated or token is invalid</response>
    /// <response code="500">If an unexpected error occurs during the recommendation generation process</response>
    [HttpGet("generate")]
    [ProducesResponseType(typeof(List<ScoredItemDTOs.ScoredItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<ActionResult<List<ScoredItemDTOs.ScoredItem>>> GenerateRecommendations(
        [FromQuery, Required, Range(1, 25)] int numberOfRecommendations,
        CancellationToken ct)
    {
        if (numberOfRecommendations <= 0)
            return BadRequest("A valid NumberOfRecommendations (> 0) is required.");

        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(value, out var userId))
            return Unauthorized("User not valid or token malformed.");

        var body = new GenerateRecommendationsDTOs.GenerateRecommendationsRequest(userId, numberOfRecommendations);

        var res = await generateRecommendationHandler.HandleAsync(body, ct);

        return Ok(res);
    }


    /// <summary>
    /// Retrieves the most recent set of recommendations previously generated for the authenticated user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of previously generated recommendations for the authenticated user</returns>
    /// <response code="200">Recommendations were successfully retrieved</response>
    /// <response code="400">If the user identifier is missing or invalid</response>
    /// <response code="401">If the user is not authenticated or the token is malformed</response>
    /// <response code="404">If the user was not found or has no stored recommendations</response>
    /// <response code="500">If an unexpected error occurs during the retrieval process</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<GenerateRecommendationsDTOs.RecommendationsItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<ActionResult<List<GenerateRecommendationsDTOs.RecommendationsItem>>> GetRecommendations(
        CancellationToken ct)
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(value, out var userId))
            return Unauthorized("User not valid or token malformed.");

        var req = new GetRecommendationDTOs.GetRecommendationRequest(userId);

        var res = await getRecommendationHandler.GetRecommendation(req);

        if (res?.Recommendations == null || !res.Recommendations.Any())
            return NotFound("No recommendations found for this user.");

        return Ok(res.Recommendations);
    }
}
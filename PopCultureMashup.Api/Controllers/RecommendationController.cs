using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Recommend;

namespace PopCultureMashup.Api.Controllers;

/// <summary>
/// Controller responsible for generating and retrieving personalized recommendations
/// </summary>
[ApiController]
[Route("/recommendations")]
[Produces("application/json")]
public class RecommendationController(
    GenerateRecommendationsHandler generateRecommendationHandler,
    GetRecommendationHandler getRecommendationHandler) : ControllerBase
{
    /// <summary>
    /// Generates new recommendations based on user profile
    /// </summary>
    /// <param name="body">User data to generate recommendations</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of recommended items with scores</returns>
    /// <response code="200">Recommendations generated successfully</response>
    /// <response code="400">Invalid UserId</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(List<ScoredItemDTOs.ScoredItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ScoredItemDTOs.ScoredItem>>> GenerateRecommendations(
        [FromBody] GenerateRecommendationsDTOs.GenerateRecommendationsRequest body,
        CancellationToken ct)
    {
        if (body is null || body.UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.");

        var res = await generateRecommendationHandler.HandleAsync(body, ct);
        return Ok(res);
    }

    /// <summary>
    /// Gets previously generated recommendations for a user
    /// </summary>
    /// <param name="userId">Unique user identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of saved recommendations for the user</returns>
    /// <response code="200">Recommendations retrieved successfully</response>
    /// <response code="400">Invalid UserId</response>
    /// <response code="404">User not found or no recommendations available</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(List<GenerateRecommendationsDTOs.RecommendationsItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<GenerateRecommendationsDTOs.RecommendationsItem>>> GetRecommendations(
        [FromRoute] Guid userId,
        CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.");

        var req = new GetRecommendationDTOs.GetRecommendationRequest(userId);

        var res = await getRecommendationHandler.GetRecommendation(req);
        return Ok(res.Recommendations);
    }
}
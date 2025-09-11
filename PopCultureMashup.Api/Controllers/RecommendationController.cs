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
    /// Generates personalized recommendations based on user preferences and history
    /// </summary>
    /// <param name="body">User profile data containing preferences and interaction history used for generating recommendations</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of recommended items (games and books) with relevance scores</returns>
    /// <response code="200">Recommendations were successfully generated based on user profile</response>
    /// <response code="400">If the request body is invalid or the UserId is empty</response>
    /// <response code="500">If an unexpected error occurs during the recommendation generation process</response>
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
    /// Retrieves the most recent set of recommendations previously generated for a specific user
    /// </summary>
    /// <param name="userId">Unique identifier of the user whose recommendations should be retrieved</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of previously generated recommendations for the specified user</returns>
    /// <response code="200">Recommendations were successfully retrieved</response>
    /// <response code="400">If the UserId is invalid or empty</response>
    /// <response code="404">If the user was not found or has no stored recommendations</response>
    /// <response code="500">If an unexpected error occurs during the retrieval process</response>
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
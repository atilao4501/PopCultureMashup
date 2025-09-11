using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Application.UseCases.Recommend;

namespace PopCultureMashup.Api.Controllers;

[ApiController]
[Route("/recommendations")]
public class RecommendationController(
    GenerateRecommendationsHandler generateRecommendationHandler,
    GetRecommendationHandler getRecommendationHandler) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult<List<ScoredItemDTOs.ScoredItem>>> GenerateRecommendations(
        [FromBody] GenerateRecommendationsDTOs.GenerateRecommendationsRequest body,
        CancellationToken ct)
    {
        if (body is null || body.UserId == Guid.Empty)
            return BadRequest("UserId is required.");

        var res = await generateRecommendationHandler.HandleAsync(body, ct);
        return Ok(res);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<List<GenerateRecommendationsDTOs.RecommendationsItem>>> GetRecommendations(
        [FromRoute] Guid userId,
        CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return BadRequest("UserId is required.");

        var req = new GetRecommendationDTOs.GetRecommendationRequest(userId);

        var res = await getRecommendationHandler.GetRecommendation(req);
        return Ok(res.Recommendations);
    }
}
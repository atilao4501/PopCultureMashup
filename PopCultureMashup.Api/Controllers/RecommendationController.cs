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
    [HttpPost]
    public async Task<ActionResult<List<ScoredItemDTOs.ScoredItem>>> GenerateRecommendations(
        [FromBody] GenerateRecommendationsDTOs.GenerateRecommendationsRequest body,
        CancellationToken ct)
    {
        if (body is null || body.UserId == Guid.Empty)
            return BadRequest("UserId is required.");

        var res = await generateRecommendationHandler.HandleAsync(body, ct);
        return Ok(res);
    }
}
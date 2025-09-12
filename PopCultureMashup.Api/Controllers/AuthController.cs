using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PopCultureMashup.Application.Abstractions.Auth;
using PopCultureMashup.Application.DTOs;

namespace PopCultureMashup.Api.Controllers;

/// <summary>
/// Authentication endpoints for user registration, login (JWT) and token refresh.
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="body">Credentials and optional display name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Basic data from the newly created user.</returns>
    /// <response code="201">User successfully created.</response>
    /// <response code="400">Invalid data or password policy not satisfied.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthDTOs.RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthDTOs.RegisterResponse>> Register(
        [FromBody] AuthDTOs.RegisterRequest body,
        CancellationToken ct)
    {
        try
        {
            var res = await auth.RegisterAsync(body, ct);
            return Created(string.Empty, res);
        }
        catch (InvalidOperationException ex)
        {
            // IAuthService lança InvalidOperationException agregando erros do Identity
            return BadRequest(new ProblemDetails { Title = "Registration failed", Detail = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user and issues a JWT access token plus a refresh token.
    /// </summary>
    /// <param name="body">Login credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Access token (JWT) and refresh token.</returns>
    /// <response code="200">Authentication succeeded.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthDTOs.LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthDTOs.LoginResponse>> Login(
        [FromBody] AuthDTOs.LoginRequest body,
        CancellationToken ct)
    {
        try
        {
            var res = await auth.LoginAsync(body, ct);
            return Ok(res);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new JWT access token and a rotated refresh token.
    /// </summary>
    /// <param name="body">The current (active) refresh token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>New access token and new refresh token.</returns>
    /// <response code="200">Tokens successfully refreshed.</response>
    /// <response code="401">Refresh token invalid or expired.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthDTOs.RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthDTOs.RefreshResponse>> Refresh(
        [FromBody] AuthDTOs.RefreshRequest body,
        CancellationToken ct)
    {
        try
        {
            var res = await auth.RefreshAsync(body, ct);
            return Ok(res);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Returns basic information about the currently authenticated user (from JWT claims).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User id, name and email extracted from the token.</returns>
    /// <response code="200">Token is valid and claims were returned.</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> Me(CancellationToken ct)
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email =
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
        var name = User.Identity?.Name;

        return Ok(new { id, name, email });
    }
}
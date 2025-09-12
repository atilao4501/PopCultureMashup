namespace PopCultureMashup.Application.DTOs;


public static class AuthDTOs
{
    public record RegisterRequest(string Email, string Password);
    public record RegisterResponse(Guid Id, string Email);

    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string AccessToken, string RefreshToken);

    public record RefreshRequest(string RefreshToken);
    public record RefreshResponse(string AccessToken, string RefreshToken);
}
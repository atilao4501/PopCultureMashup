namespace PopCultureMashup.Application.Abstractions.Auth;

public interface IJwtIssuer
{
    string CreateAccessToken(Guid userId, string? email, string? userName);
}
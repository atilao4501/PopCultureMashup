using PopCultureMashup.Application.DTOs;

namespace PopCultureMashup.Application.Abstractions.Auth;

public interface IAuthService
{
    Task<AuthDTOs.RegisterResponse> RegisterAsync(AuthDTOs.RegisterRequest req, CancellationToken ct = default);
    Task<AuthDTOs.LoginResponse>    LoginAsync(AuthDTOs.LoginRequest req, CancellationToken ct = default);
    Task<AuthDTOs.RefreshResponse>  RefreshAsync(AuthDTOs.RefreshRequest req, CancellationToken ct = default);
}
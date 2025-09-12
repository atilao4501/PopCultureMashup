using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PopCultureMashup.Application.Abstractions.Auth;
using PopCultureMashup.Application.DTOs;
using PopCultureMashup.Infrastructure.Auth.Entities;
using PopCultureMashup.Infrastructure.Persistence;
using PopCultureMashup.Infrastructure.Persistence.Entities;

namespace PopCultureMashup.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<User> users,
    SignInManager<User> signIn,
    IJwtIssuer jwt,
    AppDbContext db,
    IConfiguration cfg) : IAuthService
{
    public async Task<AuthDTOs.RegisterResponse> RegisterAsync(AuthDTOs.RegisterRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Email)) 
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(req.Password)) 
            throw new ArgumentException("Password is required.");

        var u = new User { UserName = req.Email, Email = req.Email };
        var res = await users.CreateAsync(u, req.Password);
        if (!res.Succeeded)
        {
            var errors = string.Join("; ", res.Errors.Select(e => $"{e.Code}:{e.Description}"));
            throw new InvalidOperationException($"Register failed: {errors}");
        }

        return new AuthDTOs.RegisterResponse(u.Id, u.Email);
    }


    public async Task<AuthDTOs.LoginResponse> LoginAsync(AuthDTOs.LoginRequest req, CancellationToken ct = default)
    {
        var u = await users.FindByEmailAsync(req.Email);
        if (u is null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var check = await signIn.CheckPasswordSignInAsync(u, req.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var access = jwt.CreateAccessToken(u.Id, u.Email, u.UserName);

        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = u.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(cfg["Jwt:RefreshTokenDays"]!))
        };
        db.RefreshTokens.Add(refresh);
        await db.SaveChangesAsync(ct);

        return new AuthDTOs.LoginResponse(access, refresh.Token);
    }

    public async Task<AuthDTOs.RefreshResponse> RefreshAsync(AuthDTOs.RefreshRequest req, CancellationToken ct = default)
    {
        var r = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == req.RefreshToken, ct);
        if (r is null || !r.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");
        
        if (r.RevokedAt is not null)
            throw new UnauthorizedAccessException("Refresh token has been revoked.");

        var u = await users.FindByIdAsync(r.UserId.ToString());
        if (u is null)
            throw new UnauthorizedAccessException("User not found.");

        // rotate
        r.RevokedAt = DateTime.UtcNow;
        var newRefresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = r.UserId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(cfg["Jwt:RefreshTokenDays"]!)),
            ReplacedByToken = r.Token
        };

        db.Add(newRefresh);
        await db.SaveChangesAsync(ct);

        var access = jwt.CreateAccessToken(u.Id, u.Email, u.UserName);
        return new AuthDTOs.RefreshResponse(access, newRefresh.Token);
    }
}
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TaskyV2.Application.DTOs;
using TaskyV2.Domain.Entities;
using TaskyV2.Infrastructure.Auth;
using TaskyV2.Infrastructure.Data;

namespace TaskyV2.Infrastructure.Services;

public class AuthService(AppDbContext db, ITokenService tokenService)
{
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string NormalizePassword(string password) => password.Trim();

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var norm = NormalizeEmail(email);
        return await db.Users.AnyAsync(u => u.Email == norm, ct);
    }

    public async Task RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var email = NormalizeEmail(req.Email);
        var password = NormalizePassword(req.Password);

        if (await EmailExistsAsync(email, ct))
            throw new InvalidOperationException("Email already registered.");

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Email = email, PasswordHash = hash };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var email = NormalizeEmail(req.Email);
        var password = NormalizePassword(req.Password);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = tokenService.CreateToken(user.Id, user.Email);
        return new AuthResponse(token);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace TaskyV2.Infrastructure.Auth;

public interface ITokenService
{
    string CreateToken(Guid userId, string email);
}

public class TokenService(IConfiguration config) : ITokenService
{
    public string CreateToken(Guid userId, string email)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
        var issuer = config["Jwt:Issuer"] ?? "tasky";
        var audience = config["Jwt:Audience"] ?? "tasky";

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
        };
        var token = new JwtSecurityToken(
            issuer: issuer, audience: audience, claims: claims,
            expires: DateTime.UtcNow.AddHours(2), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

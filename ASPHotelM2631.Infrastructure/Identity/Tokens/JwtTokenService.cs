using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ASPHotelM2631.Application.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ASPHotelM2631.Infrastructure.Identity.Tokens;

public sealed class JwtTokenService(IConfiguration config) : ITokenService
{
    public (string Token, DateTime ExpiresAt) CreateToken(
        string userId, string userName, string fullName,
        IEnumerable<string> roles, string securityStamp)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key in configuration");

        var minutes = int.TryParse(config["Jwt:ExpireMinutes"], out var m) ? m : 15;

        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new("sub", userId),
            new("name", userName),
            new("fullName", fullName),
            new("securityStamp", securityStamp)
        };

        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var jwt = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));
        
            return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}

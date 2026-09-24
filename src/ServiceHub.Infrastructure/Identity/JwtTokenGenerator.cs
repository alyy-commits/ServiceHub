using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceHub.Domain.Entities;

namespace ServiceHub.Infrastructure.Identity;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) Generate(ApplicationUser user, IEnumerable<string> roles)
    {
        // Short claim names ("sub", "role", ...) are used on purpose; the API disables inbound claim mapping.
        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("name", user.FullName)
        };

        List<string> roleList = roles.ToList();
        for (int i = 0; i < roleList.Count; i++)
        {
            claims.Add(new Claim("role", roleList[i]));
        }

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        DateTime expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

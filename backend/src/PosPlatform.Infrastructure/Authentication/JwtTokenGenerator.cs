using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user, IEnumerable<string> permissions)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        foreach (var perm in permissions)
            claims.Add(new Claim("perm", perm));

        if (user.AppRoleId is not null)
            claims.Add(new Claim("roleId", user.AppRoleId.Value.ToString()));

        if (user.BranchId is not null)
            claims.Add(new Claim("branchId", user.BranchId.Value.ToString()));

        if (user.OrganizationId is not null)
            claims.Add(new Claim("organizationId", user.OrganizationId.Value.ToString()));

        // Kassa biriktirilgan bo'lsa tokenga yozamiz — savdo/smena so'rovlarida boshqa
        // kassadan kelgan yozuvlarni rad etish uchun (bitta login = bitta kassa).
        if (!string.IsNullOrWhiteSpace(user.TerminalId))
            claims.Add(new Claim("terminalId", user.TerminalId.Trim()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, expiresAt);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopAuth;
using PosCloud.Domain.Entities;

namespace PosCloud.Infrastructure.Authentication;

/// <summary>Mobil ilova mijozi tokeni: sub = ShopUserId, role = ShopCustomer, audience = ShopAudience.</summary>
public class ShopTokenGenerator : IShopTokenGenerator
{
    private readonly JwtSettings _settings;

    public ShopTokenGenerator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public int RefreshTokenDays => _settings.ShopRefreshDays;

    public (string Token, DateTime ExpiresAt) GenerateToken(ShopUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ShopExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, ShopRoles.Customer),
            new("phone", user.Phone)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.ShopAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

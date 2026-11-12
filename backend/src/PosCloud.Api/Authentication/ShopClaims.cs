using System.Security.Claims;
using PosCloud.Application.Common.Exceptions;

namespace PosCloud.Api.Authentication;

public static class ShopClaims
{
    /// <summary>Mijoz tokenidagi ShopUserId (sub → NameIdentifier).</summary>
    public static Guid GetShopUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id)
            ? id
            : throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
}

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

    /// <summary>
    /// Tokensiz ochiq endpointlarda ixtiyoriy mijoz tokeni: yaroqli bo'lsa ShopUserId, aks holda null
    /// (yaroqsiz/eskirgan token xato bermaydi — ommaviy javob qaytadi).
    /// </summary>
    public static async Task<Guid?> TryGetShopUserIdAsync(this Microsoft.AspNetCore.Http.HttpContext context)
    {
        var auth = await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions
            .AuthenticateAsync(context, ShopAuthDefaults.Scheme);
        if (!auth.Succeeded || !auth.Principal.IsInRole(PosCloud.Application.Features.ShopAuth.ShopRoles.Customer))
            return null;
        return Guid.TryParse(auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? auth.Principal.FindFirstValue("sub"),
            out var id) ? id : null;
    }
}

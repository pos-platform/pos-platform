using PosCloud.Domain.Entities;

namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// Mobil ilova mijozi uchun access token. Xodim tokenidan alohida audience bilan imzolanadi —
/// shu sababli xodim endpointlari uni qabul qilmaydi va aksincha.
/// </summary>
public interface IShopTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(ShopUser user);

    /// <summary>Refresh token amal qilish muddati (kunlarda).</summary>
    int RefreshTokenDays { get; }
}

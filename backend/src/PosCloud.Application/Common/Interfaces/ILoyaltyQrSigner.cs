namespace PosCloud.Application.Common.Interfaces;

/// <summary>
/// Dinamik bonus karta QR kodi: "PCB1." + base64url(payload) + "." + base64url(HMAC-SHA256).
/// Stateless — bazaga yozilmaydi. Token loglanmaydi.
/// </summary>
public interface ILoyaltyQrSigner
{
    /// <summary>Kod yaratadi (payload: u — ShopUser, o — tashkilot, e — unix muddat).</summary>
    string Sign(Guid shopUserId, Guid organizationId, DateTime expiresAtUtc);

    /// <summary>Faqat format va imzoni tekshiradi (doimiy vaqtli taqqoslash). Muddatni chaqiruvchi tekshiradi.</summary>
    bool TryVerify(string code, out Guid shopUserId, out Guid organizationId, out DateTime expiresAtUtc);
}

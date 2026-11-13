using PosCloud.Application.Common.Exceptions;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlineMarketing;

/// <summary>Banner/story/push havolasini tekshirish va normallashtirish.</summary>
public static class ShopLinks
{
    /// <summary>Product/Category/Collection — Guid; Url — to'liq https havola; None/Discounts — qiymatsiz (null).</summary>
    public static string? Normalize(ShopLinkType type, string? value)
    {
        if (!Enum.IsDefined(type))
            throw new ValidationException("Havola turi noto'g'ri.");

        var v = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        switch (type)
        {
            case ShopLinkType.None:
            case ShopLinkType.Discounts:
                return null;
            case ShopLinkType.Product:
            case ShopLinkType.Category:
            case ShopLinkType.Collection:
                if (v is null || !Guid.TryParse(v, out var id))
                    throw new ValidationException("Havola uchun mahsulot/kategoriya/kolleksiyani tanlang.");
                return id.ToString();
            case ShopLinkType.Url:
                if (v is null || v.Length > 500 || !Uri.TryCreate(v, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    throw new ValidationException("Havola https:// bilan boshlanadigan to'liq manzil bo'lishi kerak.");
                return v;
            default:
                return null;
        }
    }
}

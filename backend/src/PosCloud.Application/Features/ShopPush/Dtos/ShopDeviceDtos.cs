using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.ShopPush.Dtos;

public class RegisterShopDeviceRequest
{
    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    /// <summary>"android" yoki "ios".</summary>
    [Required]
    [MaxLength(10)]
    public string Platform { get; set; } = string.Empty;

    /// <summary>"uz" yoki "ru" (boshqasi — "uz").</summary>
    [MaxLength(5)]
    public string? Locale { get; set; }
}

/// <summary>Navbatdagi push vazifasi: buyurtma holati o'zgardi (mijoz ma'lumotisiz; kuryer — faqat "Yo'lda" matni uchun).</summary>
public record OrderStatusPush(Guid OrderId, Guid ShopUserId, int Number, OnlineOrderStatus Status,
    FulfillmentType FulfillmentType, string? Reason, string? CourierName = null, string? CourierPhone = null,
    decimal? Cashback = null);

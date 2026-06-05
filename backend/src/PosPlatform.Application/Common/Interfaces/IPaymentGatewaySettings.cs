namespace PosPlatform.Application.Common.Interfaces;

public interface IPaymentGatewaySettings
{
    string? PaymeMerchantId { get; }
    string? PaymeSecretKey { get; }
    string? ClickServiceId { get; }
    string? ClickMerchantId { get; }
    string? ClickSecretKey { get; }
}

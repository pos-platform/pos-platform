using PosCloud.Application.Common.Interfaces;

namespace PosCloud.Api.Services;

public class PaymentGatewaySettings : IPaymentGatewaySettings
{
    public string? PaymeMerchantId { get; }
    public string? PaymeSecretKey { get; }
    public string? ClickServiceId { get; }
    public string? ClickMerchantId { get; }
    public string? ClickSecretKey { get; }

    public PaymentGatewaySettings(IConfiguration config)
    {
        PaymeMerchantId = config["Payme:MerchantId"];
        PaymeSecretKey = config["Payme:SecretKey"];
        ClickServiceId = config["Click:ServiceId"];
        ClickMerchantId = config["Click:MerchantId"];
        ClickSecretKey = config["Click:SecretKey"];
    }
}

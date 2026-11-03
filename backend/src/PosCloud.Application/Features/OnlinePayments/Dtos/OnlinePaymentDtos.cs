using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.OnlinePayments.Dtos;

public class OnlinePaymentDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static OnlinePaymentDto FromEntity(OnlinePayment p) => new()
    {
        Id = p.Id,
        SaleId = p.SaleId,
        Provider = p.Provider.ToString(),
        Status = p.Status.ToString(),
        ExternalId = p.ExternalId,
        Amount = p.Amount,
        PaymentUrl = p.PaymentUrl,
        PaidAt = p.PaidAt,
        CreatedAt = p.CreatedAt
    };
}

public class CreateOnlinePaymentRequest
{
    [Required]
    public Guid SaleId { get; set; }

    [Required]
    public PaymentProvider Provider { get; set; }
}

// Payme Subscribe API RPC
public class PaymeRpcRequest
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }

    [JsonPropertyName("id")]
    public object? Id { get; set; }
}

// Click webhook DTOs
public class ClickPrepareRequest
{
    [JsonPropertyName("click_trans_id")]
    public long ClickTransId { get; set; }

    [JsonPropertyName("service_id")]
    public long ServiceId { get; set; }

    [JsonPropertyName("merchant_trans_id")]
    public string MerchantTransId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("action")]
    public int Action { get; set; }

    [JsonPropertyName("sign_time")]
    public string SignTime { get; set; } = string.Empty;

    [JsonPropertyName("sign_string")]
    public string SignString { get; set; } = string.Empty;
}

public class ClickCompleteRequest : ClickPrepareRequest
{
    [JsonPropertyName("merchant_prepare_id")]
    public long MerchantPrepareId { get; set; }

    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("error_note")]
    public string ErrorNote { get; set; } = string.Empty;
}

public class ClickPrepareResponse
{
    [JsonPropertyName("click_trans_id")]
    public long ClickTransId { get; set; }

    [JsonPropertyName("merchant_trans_id")]
    public string MerchantTransId { get; set; } = string.Empty;

    [JsonPropertyName("merchant_prepare_id")]
    public long MerchantPrepareId { get; set; }

    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("error_note")]
    public string ErrorNote { get; set; } = string.Empty;
}

public class ClickCompleteResponse
{
    [JsonPropertyName("click_trans_id")]
    public long ClickTransId { get; set; }

    [JsonPropertyName("merchant_trans_id")]
    public string MerchantTransId { get; set; } = string.Empty;

    [JsonPropertyName("merchant_confirm_id")]
    public long MerchantConfirmId { get; set; }

    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("error_note")]
    public string ErrorNote { get; set; } = string.Empty;
}

using System.Text.Json.Serialization;

namespace FaJuan.Api.Infrastructure.WeChatPay;

public class WeChatJsapiPrepayRequest
{
    [JsonPropertyName("appid")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("mchid")]
    public string MerchantId { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("out_trade_no")]
    public string OutTradeNo { get; set; } = string.Empty;

    [JsonPropertyName("notify_url")]
    public string NotifyUrl { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public WeChatAmount Amount { get; set; } = new();

    [JsonPropertyName("payer")]
    public WeChatPayer Payer { get; set; } = new();
}

public class WeChatAmount
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "CNY";
}

public class WeChatPayer
{
    [JsonPropertyName("openid")]
    public string OpenId { get; set; } = string.Empty;
}

public class WeChatJsapiPrepayResponse
{
    [JsonPropertyName("prepay_id")]
    public string? PrepayId { get; set; }
}

public class WeChatPayCallbackEnvelope
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }

    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("resource")]
    public WeChatPayResource? Resource { get; set; }
}

public class WeChatPayResource
{
    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; set; }

    [JsonPropertyName("ciphertext")]
    public string? CipherText { get; set; }

    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }

    [JsonPropertyName("associated_data")]
    public string? AssociatedData { get; set; }
}

public class WeChatTransactionResource
{
    [JsonPropertyName("out_trade_no")]
    public string? OutTradeNo { get; set; }

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("trade_state")]
    public string? TradeState { get; set; }
}

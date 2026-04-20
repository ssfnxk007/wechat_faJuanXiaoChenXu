namespace FaJuan.Api.Infrastructure.WeChatPay;

public class WeChatPaySettingsSnapshot
{
    public string AppId { get; init; } = string.Empty;
    public string MerchantId { get; init; } = string.Empty;
    public string MerchantSerialNo { get; init; } = string.Empty;
    public string PrivateKeyPem { get; init; } = string.Empty;
    public string ApiV3Key { get; init; } = string.Empty;
    public string NotifyUrl { get; init; } = string.Empty;
    public bool EnableMockFallback { get; init; }
    public DateTime UpdatedAt { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AppId)
        && !string.IsNullOrWhiteSpace(MerchantId)
        && !string.IsNullOrWhiteSpace(MerchantSerialNo)
        && !string.IsNullOrWhiteSpace(PrivateKeyPem)
        && !string.IsNullOrWhiteSpace(ApiV3Key)
        && !string.IsNullOrWhiteSpace(NotifyUrl);
}

public class WeChatPaySettingsUpdate
{
    public string? AppId { get; set; }
    public string? MerchantId { get; set; }
    public string? MerchantSerialNo { get; set; }
    public string? PrivateKeyPem { get; set; }
    public string? ApiV3Key { get; set; }
    public string? NotifyUrl { get; set; }
    public bool? EnableMockFallback { get; set; }
}

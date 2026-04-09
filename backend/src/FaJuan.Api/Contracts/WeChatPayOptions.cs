namespace FaJuan.Api.Contracts;

public class WeChatPayOptions
{
    public string AppId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantSerialNo { get; set; } = string.Empty;
    public string PrivateKeyPemPath { get; set; } = string.Empty;
    public string ApiV3Key { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public bool EnableMockFallback { get; set; } = true;
}

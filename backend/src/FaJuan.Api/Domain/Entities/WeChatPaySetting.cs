namespace FaJuan.Api.Domain.Entities;

public class WeChatPaySetting
{
    public int Id { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantSerialNo { get; set; } = string.Empty;
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string ApiV3Key { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public bool EnableMockFallback { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}

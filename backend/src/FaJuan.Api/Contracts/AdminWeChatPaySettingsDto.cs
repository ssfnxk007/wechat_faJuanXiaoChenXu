namespace FaJuan.Api.Contracts;

public class AdminWeChatPaySettingsDto
{
    public string AppId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantSerialNo { get; set; } = string.Empty;
    public string PrivateKeyDisplay { get; set; } = string.Empty;
    public string ApiV3KeyDisplay { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public bool EnableMockFallback { get; set; }
    public bool IsConfigured { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

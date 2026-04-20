namespace FaJuan.Api.Contracts;

public class SaveAdminWeChatPaySettingsRequest
{
    public string? AppId { get; set; }
    public string? MerchantId { get; set; }
    public string? MerchantSerialNo { get; set; }
    public string? PrivateKeyPem { get; set; }
    public string? ApiV3Key { get; set; }
    public string? NotifyUrl { get; set; }
    public bool? EnableMockFallback { get; set; }
}

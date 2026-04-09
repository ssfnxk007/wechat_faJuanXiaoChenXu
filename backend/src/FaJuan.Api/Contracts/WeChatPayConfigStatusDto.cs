namespace FaJuan.Api.Contracts;

public class WeChatPayConfigStatusDto
{
    public bool IsConfigured { get; init; }
    public bool EnableMockFallback { get; init; }
    public string AppIdPreview { get; init; } = string.Empty;
    public string MerchantIdPreview { get; init; } = string.Empty;
    public string NotifyUrl { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

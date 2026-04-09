namespace FaJuan.Api.Contracts;

public class WeChatConfigStatusDto
{
    public bool IsConfigured { get; init; }
    public string AppIdPreview { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

namespace FaJuan.Api.Contracts;

public class MiniAppSessionDto
{
    public long UserId { get; set; }
    public string MiniOpenId { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Nickname { get; set; }
}

namespace FaJuan.Api.Contracts;

public class MiniProgramLoginRequest
{
    public string Code { get; init; } = string.Empty;
    public string? Nickname { get; init; }
}

public class BindMobileRequest
{
    public long UserId { get; init; }
    public string Mobile { get; init; } = string.Empty;
}

public class AuthLoginResultDto
{
    public long UserId { get; init; }
    public string MiniOpenId { get; init; } = string.Empty;
    public string? Mobile { get; init; }
    public bool IsNewUser { get; init; }
}

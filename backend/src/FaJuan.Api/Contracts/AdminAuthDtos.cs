namespace FaJuan.Api.Contracts;

public class AdminLoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class AdminLoginResultDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
}

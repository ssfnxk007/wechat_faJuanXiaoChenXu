namespace FaJuan.Api.Contracts;

public class UserListItemDto
{
    public long Id { get; init; }
    public string MiniOpenId { get; init; } = string.Empty;
    public string? Mobile { get; init; }
    public string? Nickname { get; init; }
    public DateTime CreatedAt { get; init; }
}

namespace FaJuan.Api.Domain.Entities;

public class AppUser
{
    public long Id { get; set; }
    public string MiniOpenId { get; set; } = string.Empty;
    public string? UnionId { get; set; }
    public string? OfficialOpenId { get; set; }
    public string? Mobile { get; set; }
    public string? Nickname { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

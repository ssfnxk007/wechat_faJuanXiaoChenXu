namespace FaJuan.Api.Domain.Entities;

public class AdminRoleMenu
{
    public long Id { get; set; }
    public long AdminRoleId { get; set; }
    public long AdminMenuId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

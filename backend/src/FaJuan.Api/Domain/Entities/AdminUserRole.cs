namespace FaJuan.Api.Domain.Entities;

public class AdminUserRole
{
    public long Id { get; set; }
    public long AdminUserId { get; set; }
    public long AdminRoleId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

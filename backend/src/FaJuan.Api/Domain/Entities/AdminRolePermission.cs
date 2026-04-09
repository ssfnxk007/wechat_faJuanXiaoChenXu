namespace FaJuan.Api.Domain.Entities;

public class AdminRolePermission
{
    public long Id { get; set; }
    public long AdminRoleId { get; set; }
    public long AdminPermissionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

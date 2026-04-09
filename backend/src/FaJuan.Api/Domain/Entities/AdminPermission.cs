namespace FaJuan.Api.Domain.Entities;

public class AdminPermission
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string MenuPath { get; set; } = string.Empty;
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

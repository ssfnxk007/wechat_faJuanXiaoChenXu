namespace FaJuan.Api.Domain.Entities;

public class AdminMenu
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

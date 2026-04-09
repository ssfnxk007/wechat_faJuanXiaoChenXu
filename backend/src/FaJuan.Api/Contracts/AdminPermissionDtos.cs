using FaJuan.Api.Application.Common.Models;

namespace FaJuan.Api.Contracts;

public class AdminUserListItemDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyCollection<long> RoleIds { get; set; } = Array.Empty<long>();
    public string RoleNames { get; set; } = string.Empty;
}

public class SaveAdminUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string? Password { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public IReadOnlyCollection<long> RoleIds { get; init; } = Array.Empty<long>();
}

public class ResetAdminUserPasswordRequest
{
    public string Password { get; init; } = string.Empty;
}

public class AdminRoleListItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int MenuCount { get; set; }
    public int PermissionCount { get; set; }
    public IReadOnlyCollection<long> MenuIds { get; set; } = Array.Empty<long>();
    public IReadOnlyCollection<long> PermissionIds { get; set; } = Array.Empty<long>();
}

public class SaveAdminRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public IReadOnlyCollection<long> MenuIds { get; init; } = Array.Empty<long>();
    public IReadOnlyCollection<long> PermissionIds { get; init; } = Array.Empty<long>();
}

public class AdminPermissionListItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string MenuPath { get; set; } = string.Empty;
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminMenuListItemDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyCollection<AdminMenuListItemDto> Children { get; set; } = Array.Empty<AdminMenuListItemDto>();
}

public class SaveAdminMenuRequest
{
    public long? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Component { get; init; } = string.Empty;
    public int Sort { get; init; }
    public bool IsEnabled { get; init; } = true;
}

public class AdminAuthProfileDto
{
    public long UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsFallbackAdmin { get; init; }
    public IReadOnlyCollection<string> RoleCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> RoleNames { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> MenuPaths { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> PermissionCodes { get; init; } = Array.Empty<string>();
}

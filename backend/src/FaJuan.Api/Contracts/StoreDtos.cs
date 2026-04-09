namespace FaJuan.Api.Contracts;

public class StoreListItemDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class SaveStoreRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public bool IsEnabled { get; init; } = true;
}

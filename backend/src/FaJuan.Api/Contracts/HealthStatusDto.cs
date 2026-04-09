namespace FaJuan.Api.Contracts;

public class HealthStatusDto
{
    public string Service { get; init; } = "FaJuan.Api";
    public string Version { get; init; } = "v1";
    public string Environment { get; init; } = string.Empty;
    public DateTimeOffset ServerTime { get; init; }
}

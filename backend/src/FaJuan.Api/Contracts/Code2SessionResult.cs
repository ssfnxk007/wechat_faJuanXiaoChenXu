namespace FaJuan.Api.Contracts;

public class Code2SessionResult
{
    public string? OpenId { get; init; }
    public string? SessionKey { get; init; }
    public string? UnionId { get; init; }
    public int? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsSuccess => ErrorCode is null or 0 && !string.IsNullOrWhiteSpace(OpenId);
}

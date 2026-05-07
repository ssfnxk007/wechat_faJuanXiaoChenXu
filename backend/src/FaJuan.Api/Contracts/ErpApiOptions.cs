namespace FaJuan.Api.Contracts;

public class ErpApiOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string ApiKeyHeaderName { get; init; } = "X-Api-Key";
}

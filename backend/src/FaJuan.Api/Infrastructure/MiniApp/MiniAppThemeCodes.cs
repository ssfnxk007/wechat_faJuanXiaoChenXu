namespace FaJuan.Api.Infrastructure.MiniApp;

public static class MiniAppThemeCodes
{
    public const string Green = "green";
    public const string Light = "light";

    public static readonly string[] All = [Green, Light];

    public static string Normalize(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return All.Contains(normalized) ? normalized! : Green;
    }
}

namespace FaJuan.Api.Infrastructure.MiniApp;

public static class MiniAppThemeCodes
{
    public const string Green = "green";
    public const string Light = "light";
    public const string Candy = "candy";
    public const string Orange = "orange";
    public const string Red = "red";

    public static readonly string[] All = [Green, Light, Candy, Orange, Red];

    public static string Normalize(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return All.Contains(normalized) ? normalized! : Candy;
    }
}

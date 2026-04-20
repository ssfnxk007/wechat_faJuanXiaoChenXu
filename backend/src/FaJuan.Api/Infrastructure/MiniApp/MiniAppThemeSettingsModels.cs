namespace FaJuan.Api.Infrastructure.MiniApp;

public class MiniAppThemeSettingsSnapshot
{
    public string ThemeCode { get; init; } = MiniAppThemeCodes.Candy;
}

public class MiniAppThemeSettingsFileModel
{
    public string ThemeCode { get; set; } = MiniAppThemeCodes.Candy;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class MiniAppThemeSettingsOptions
{
    public string ThemeCode { get; set; } = MiniAppThemeCodes.Candy;
}

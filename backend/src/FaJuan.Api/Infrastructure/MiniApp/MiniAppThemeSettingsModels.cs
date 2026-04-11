namespace FaJuan.Api.Infrastructure.MiniApp;

public class MiniAppThemeSettingsSnapshot
{
    public string ThemeCode { get; init; } = MiniAppThemeCodes.Green;
}

public class MiniAppThemeSettingsFileModel
{
    public string ThemeCode { get; set; } = MiniAppThemeCodes.Green;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class MiniAppThemeSettingsOptions
{
    public string ThemeCode { get; set; } = MiniAppThemeCodes.Green;
}

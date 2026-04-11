using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FaJuan.Api.Infrastructure.MiniApp;

public class MiniAppThemeSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly string _defaultThemeCode;

    public MiniAppThemeSettingsService(IWebHostEnvironment environment, IOptions<MiniAppThemeSettingsOptions> options)
    {
        var appDataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(appDataDirectory);
        _settingsPath = Path.Combine(appDataDirectory, "miniapp-theme-settings.json");
        _defaultThemeCode = MiniAppThemeCodes.Normalize(options.Value.ThemeCode);
    }

    public async Task<MiniAppThemeSettingsSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var model = await ReadAsync(cancellationToken);
        return new MiniAppThemeSettingsSnapshot
        {
            ThemeCode = MiniAppThemeCodes.Normalize(model.ThemeCode),
        };
    }

    public async Task<MiniAppThemeSettingsSnapshot> SaveAsync(string? themeCode, CancellationToken cancellationToken = default)
    {
        var normalizedThemeCode = MiniAppThemeCodes.Normalize(themeCode);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var model = new MiniAppThemeSettingsFileModel
            {
                ThemeCode = normalizedThemeCode,
                UpdatedAt = DateTime.Now,
            };

            var json = JsonSerializer.Serialize(model, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json, cancellationToken);

            return new MiniAppThemeSettingsSnapshot
            {
                ThemeCode = normalizedThemeCode,
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<MiniAppThemeSettingsFileModel> ReadAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_settingsPath))
            {
                var defaultModel = new MiniAppThemeSettingsFileModel
                {
                    ThemeCode = _defaultThemeCode,
                    UpdatedAt = DateTime.Now,
                };

                var json = JsonSerializer.Serialize(defaultModel, JsonOptions);
                await File.WriteAllTextAsync(_settingsPath, json, cancellationToken);
                return defaultModel;
            }

            var jsonText = await File.ReadAllTextAsync(_settingsPath, cancellationToken);
            var model = JsonSerializer.Deserialize<MiniAppThemeSettingsFileModel>(jsonText, JsonOptions);
            return model is null
                ? new MiniAppThemeSettingsFileModel { ThemeCode = _defaultThemeCode }
                : new MiniAppThemeSettingsFileModel
                {
                    ThemeCode = MiniAppThemeCodes.Normalize(model.ThemeCode),
                    UpdatedAt = model.UpdatedAt,
                };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

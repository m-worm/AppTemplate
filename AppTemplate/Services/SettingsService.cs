using System.IO.IsolatedStorage;
using System.Text.Json;
using AppTemplate.Models;
using Microsoft.Extensions.Logging;

namespace AppTemplate.Services;

public class SettingsService : ISettingsService
{
    private const string FileName = "settings.json";
    private readonly ILogger<SettingsService> _logger;
    private AppSettings _current = new();

    public SettingsService(ILogger<SettingsService> logger) => _logger = logger;

    public AppSettings Current => _current;

    public async Task LoadAsync()
    {
        try
        {
            using var store = IsolatedStorageFile.GetUserStoreForApplication();
            if (!store.FileExists(FileName)) return;

            await using var stream = store.OpenFile(FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream).ConfigureAwait(false);
            if (settings != null) _current = settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings; using defaults");
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            using var store = IsolatedStorageFile.GetUserStoreForApplication();
            await using var stream = store.OpenFile(FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            await JsonSerializer.SerializeAsync(stream, _current).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings");
        }
    }
}

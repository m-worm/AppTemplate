using System.IO.IsolatedStorage;
using System.Text.Json;
using AppTemplate.Models;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace AppTemplate.Services;

public class WindowStateService : IWindowStateService
{
    private const string FileName = "windowstate.json";
    private readonly ILogger<WindowStateService> _logger;

    public WindowStateService(ILogger<WindowStateService> logger) => _logger = logger;

    public async Task RestoreAsync(Window window)
    {
        if (OperatingSystem.IsBrowser()) return;
        try
        {
            using var store = IsolatedStorageFile.GetUserStoreForApplication();
            if (!store.FileExists(FileName)) return;

            await using var stream = store.OpenFile(FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            var state = await JsonSerializer.DeserializeAsync<SavedWindowState>(stream);
            if (state == null) return;

            window.Width  = state.Width  > 0 ? state.Width  : 1200;
            window.Height = state.Height > 0 ? state.Height : 800;

            if (state.IsMaximized)
            {
                window.WindowState = WindowState.Maximized;
            }
            else if (state.X.HasValue && state.Y.HasValue)
            {
                var pos = new PixelPoint((int)state.X.Value, (int)state.Y.Value);
                // Check that the window's center point lands on a real screen, not just the top-left corner.
                // This catches the case where coordinates are near a screen edge after a monitor layout change.
                var center = new PixelPoint(
                    pos.X + (int)(window.Width / 2),
                    pos.Y + (int)(window.Height / 2));
                var onScreen = window.Screens.All.Any(s => s.Bounds.Contains(center));
                if (onScreen)
                    window.Position = pos;
                else
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            window.Activate();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore window state");
        }
    }

    public async Task SaveAsync(Window window)
    {
        if (OperatingSystem.IsBrowser()) return;
        try
        {
            var maximized = window.WindowState == WindowState.Maximized;
            var state = new SavedWindowState
            {
                Width       = window.Width,
                Height      = window.Height,
                IsMaximized = maximized,
                X           = maximized ? null : window.Position.X,
                Y           = maximized ? null : window.Position.Y
            };

            using var store = IsolatedStorageFile.GetUserStoreForApplication();
            await using var stream = store.OpenFile(FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            await JsonSerializer.SerializeAsync(stream, state).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save window state");
        }
    }
}

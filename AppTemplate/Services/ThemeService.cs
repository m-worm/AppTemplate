using AppTemplate.Models;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using Microsoft.Extensions.Logging;

namespace AppTemplate.Services;

public class ThemeService : IThemeService
{
    private readonly ISettingsService _settings;
    private readonly ILogger<ThemeService> _logger;

    public ThemeService(ISettingsService settings, ILogger<ThemeService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public AppTheme CurrentTheme => _settings.Current.Theme;

    public AccentColorMode CurrentAccentMode => _settings.Current.AccentColorMode;

    public Color? CurrentCustomAccentColor
    {
        get
        {
            if (_settings.Current.CustomAccentColor is { } hex &&
                Color.TryParse(hex, out var color))
                return color;
            return null;
        }
    }

    public void ApplyCurrentTheme() => ApplyTheme(_settings.Current.Theme);

    public void ApplyAccentColor()
    {
        var faTheme = Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        if (faTheme == null) return;

        if (_settings.Current.AccentColorMode == AccentColorMode.System)
        {
            faTheme.PreferUserAccentColor = true;
            faTheme.CustomAccentColor = null;
        }
        else
        {
            faTheme.PreferUserAccentColor = false;
            faTheme.CustomAccentColor = CurrentCustomAccentColor;
        }

        _logger.LogInformation("Accent mode: {Mode}", _settings.Current.AccentColorMode);
    }

    public void SetTheme(AppTheme theme)
    {
        _settings.Current.Theme = theme;
        ApplyTheme(theme);
        _ = _settings.SaveAsync();
    }

    public void SetAccentColor(AccentColorMode mode, Color? customColor = null)
    {
        _settings.Current.AccentColorMode = mode;

        if (mode == AccentColorMode.Custom && customColor.HasValue)
            _settings.Current.CustomAccentColor = $"#{customColor.Value.R:X2}{customColor.Value.G:X2}{customColor.Value.B:X2}";
        else if (mode == AccentColorMode.System)
            _settings.Current.CustomAccentColor = null;

        ApplyAccentColor();
        _ = _settings.SaveAsync();
    }

    private void ApplyTheme(AppTheme theme)
    {
        if (Application.Current == null) return;

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark  => ThemeVariant.Dark,
            _              => ThemeVariant.Default
        };

        _logger.LogInformation("Theme set to {Theme}", theme);
    }
}

using AppTemplate.Models;
using Avalonia.Media;

namespace AppTemplate.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    AccentColorMode CurrentAccentMode { get; }
    Color? CurrentCustomAccentColor { get; }
    void ApplyCurrentTheme();
    void ApplyAccentColor();
    void SetTheme(AppTheme theme);
    void SetAccentColor(AccentColorMode mode, Color? customColor = null);
}

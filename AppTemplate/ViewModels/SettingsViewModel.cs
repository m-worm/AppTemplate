using AppTemplate.Models;
using AppTemplate.Services;
using AppTemplate.ViewModels.Base;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;

namespace AppTemplate.ViewModels;

public partial class SettingsViewModel : PageViewModelBase
{
    private readonly IThemeService _themes;

    // ── Theme ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLightTheme))]
    [NotifyPropertyChangedFor(nameof(IsDarkTheme))]
    [NotifyPropertyChangedFor(nameof(IsAutoTheme))]
    private AppTheme _selectedTheme;

    public bool IsLightTheme
    {
        get => SelectedTheme == AppTheme.Light;
        set { if (value) SelectedTheme = AppTheme.Light; }
    }

    public bool IsDarkTheme
    {
        get => SelectedTheme == AppTheme.Dark;
        set { if (value) SelectedTheme = AppTheme.Dark; }
    }

    public bool IsAutoTheme
    {
        get => SelectedTheme == AppTheme.Auto;
        set { if (value) SelectedTheme = AppTheme.Auto; }
    }

    partial void OnSelectedThemeChanged(AppTheme value) => _themes.SetTheme(value);

    // ── Accent colour ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomAccentEnabled))]
    private bool _useSystemAccentColor;

    [ObservableProperty]
    private Color _customAccentColor;

    public bool IsCustomAccentEnabled => !UseSystemAccentColor;

    partial void OnUseSystemAccentColorChanged(bool value)
    {
        var mode = value ? AccentColorMode.System : AccentColorMode.Custom;
        _themes.SetAccentColor(mode, value ? null : CustomAccentColor);
    }

    partial void OnCustomAccentColorChanged(Color value)
    {
        if (!UseSystemAccentColor)
            _themes.SetAccentColor(AccentColorMode.Custom, value);
    }

    // ── About ────────────────────────────────────────────────────────────────

    public string AppVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    public string AppName =>
        Assembly.GetEntryAssembly()?.GetName().Name ?? "AppTemplate";

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsViewModel(IThemeService themes)
    {
        _themes = themes;
        Title = "Settings";
        _selectedTheme  = themes.CurrentTheme;
        _useSystemAccentColor = themes.CurrentAccentMode == AccentColorMode.System;
        _customAccentColor = themes.CurrentCustomAccentColor ?? Color.Parse("#0078D4");
    }
}

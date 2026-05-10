namespace AppTemplate.Models;

public enum AppTheme { Light, Dark, Auto }

public enum AccentColorMode { System, Custom }

public class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Auto;
    public AccentColorMode AccentColorMode { get; set; } = AccentColorMode.System;
    public string? CustomAccentColor { get; set; }
}

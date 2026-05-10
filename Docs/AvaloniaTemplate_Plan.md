# Avalonia 12 + FluentAvalonia Application Template — Implementation Plan

**Created:** 2026-05-04  
**Status:** FINAL — all decisions resolved, ready to build

---

## Resolved Configuration

| # | Decision | Answer |
|---|---|---|
| 1 | MVVM toolkit | CommunityToolkit.Mvvm |
| 2 | Settings persistence | IsolatedStorage (unified for Desktop + WASM) |
| 3 | WASM hosting | Localhost dev only |
| 4 | HelloWorld dialog | FluentAvalonia `ContentDialog` |
| 5 | Project / namespace | `AppTemplate` / `MyApp` |
| 6 | Extra placeholder pages | None — Home + HelloWorld sufficient |
| 7 | NLog targets | File only |
| 8 | Desktop packaging | Framework-dependent (`dotnet run`) |
| 9 | Included suggestions | A (global exception handler), C (settings persistence), G (window state) |

---

## Technology Stack

| Layer | Package | Version |
|---|---|---|
| Runtime | .NET 10, C# 13 | 10.x |
| UI Framework | Avalonia | **12.0.0** (pinned — FA 3.0 incompatible with 12.0.2) |
| Fluent Controls | FluentAvaloniaUI | **3.0.0-preview2** (FA v3 targets Avalonia 12 + .NET 10; all types prefixed `FA`) |
| Icons | FluentIcons.Avalonia.Fluent | 2.0.325 |
| MVVM | CommunityToolkit.Mvvm | 8.x |
| DI Container | Microsoft.Extensions.DependencyInjection | 10.x |
| Logging Abstraction | Microsoft.Extensions.Logging | 10.x |
| Logging Provider | NLog + NLog.Extensions.Logging | 5.x |
| Settings Storage | System.IO.IsolatedStorage (built-in) + System.Text.Json | 10.x |

---

## Project Structure

```
AppTemplate/
├── AppTemplate.sln
├── AppTemplate/                              ← Shared Avalonia project (namespace: MyApp)
│   ├── Assets/
│   │   ├── avalonia-logo.ico
│   │   └── nlog.config
│   ├── Models/
│   │   ├── AppSettings.cs                    ← Theme enum + serializable prefs
│   │   └── SavedWindowState.cs               ← Width, Height, X, Y, IsMaximized
│   ├── ViewModels/
│   │   ├── Base/
│   │   │   ├── ViewModelBase.cs              ← ObservableObject base
│   │   │   └── PageViewModelBase.cs          ← adds Title, IconKey
│   │   ├── MainViewModel.cs                  ← nav state, IsPaneOpen, CurrentPage
│   │   ├── HomeViewModel.cs
│   │   ├── HelloWorldViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainView.axaml                    ← NavigationView shell
│   │   ├── HomeView.axaml
│   │   ├── HelloWorldView.axaml
│   │   └── SettingsView.axaml
│   ├── Services/
│   │   ├── INavigationService.cs
│   │   ├── NavigationService.cs              ← ViewModel-switching, no Frame
│   │   ├── IThemeService.cs
│   │   ├── ThemeService.cs                   ← FluentAvaloniaTheme wrapper
│   │   ├── ISettingsService.cs
│   │   ├── SettingsService.cs                ← IsolatedStorage JSON persistence
│   │   ├── IWindowStateService.cs
│   │   └── WindowStateService.cs             ← persist/restore window bounds
│   ├── Converters/                           ← shared value converters
│   ├── App.axaml                             ← DataTemplates, FluentAvaloniaTheme
│   ├── App.axaml.cs                          ← DI host bootstrap + exception handlers
│   └── ViewLocator.cs                        ← convention-based VM → View mapping
├── AppTemplate.Desktop/                      ← Desktop entry point
│   ├── Program.cs
│   └── AppTemplate.Desktop.csproj
└── AppTemplate.Browser/                      ← WASM entry point
    ├── Program.cs
    └── AppTemplate.Browser.csproj
```

---

## Shell Layout — NavigationView

The `NavigationView` from FluentAvalonia is the top-level shell in `MainView.axaml`.
It provides the collapsible left pane and the hamburger toggle natively — no custom implementation needed.

```
┌─────────────────────────────────────────────────────┐
│  ☰  AppTemplate                                     │  ← TitleBar / hamburger toggle
├──────────┬──────────────────────────────────────────┤
│ [⌂] Home │                                          │
│ [👋] Hello│          Content Area                   │
│          │          ContentControl bound to          │
│          │          MainViewModel.CurrentPage        │
│ ─────────│                                          │
│ [⚙] Sett │                                          │
└──────────┴──────────────────────────────────────────┘
```

**NavigationView configuration:**
- `DisplayMode = Auto` — expands to labeled items when wide, collapses to icon-only compact on narrow, fully hides on minimal (WASM mobile)
- `IsPaneOpen` bound two-way to `MainViewModel.IsPaneOpen`
- Settings placed in `FooterMenuItems` (pinned to bottom of pane)
- Content area is a `ContentControl` with `DataTemplates` in `App.axaml` auto-resolving `*ViewModel` → `*View`
- All nav item icons use FluentIcons via `{ic:FluentIcon Home24Regular}` markup extension

---

## MVVM Navigation Pattern

No `Frame` control — pure ViewModel switching (FluentAvalonia's recommended MVVM approach):

```csharp
// MainViewModel.cs
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private PageViewModelBase _currentPage = null!;
    [ObservableProperty] private bool _isPaneOpen = true;

    private readonly INavigationService _navigation;
    private readonly ILogger<MainViewModel> _logger;

    public void NavigateTo(PageViewModelBase page)
    {
        CurrentPage = page;
        _logger.LogInformation("Navigated to {Page}", page.GetType().Name);
    }
}
```

`ViewLocator` resolves `HomeViewModel` → `HomeView` by naming convention (strips "ViewModel", appends "View").
`DataTemplates` in `App.axaml` declare the mappings; Avalonia's `ContentControl` picks the right view automatically.
`INavigationService` is constructor-injected into any ViewModel that needs to trigger navigation.

---

## Theme Service

```csharp
public enum AppTheme { Light, Dark, Auto }

public interface IThemeService
{
    AppTheme Current { get; }
    void SetTheme(AppTheme theme);
}
```

- Light/Dark: sets `Application.Current.RequestedThemeVariant` directly
- Auto: subscribes to `IPlatformSettings.ColorValuesChanged` for real-time OS theme following; unsubscribes when switching away from Auto
- On startup: `ISettingsService` is loaded first; theme is applied before first render to prevent flash
- Standard Fluent color tokens and typography — no custom palette overrides

---

## Settings Panel

- `SettingsView.axaml` — three `RadioButton` items: Light / Dark / Auto
- Bound to `SettingsViewModel.SelectedTheme` (two-way) → `IThemeService.SetTheme()` on change
- Each change immediately triggers `ISettingsService.SaveAsync()` (fire-and-forget, errors logged)
- App name and version displayed, read from `Assembly.GetEntryAssembly()`
- About section with placeholder for app description and link

---

## HelloWorld Page

- `TextBox` bound to `HelloWorldViewModel.Name`
- "Say Hello" `Button` bound to `SayHelloCommand` (async `RelayCommand`)
- `SayHelloCommand` opens a FluentAvalonia `ContentDialog`:
  - Title: `"Hello!"`
  - Content: `Hello "{Name}"!`
  - Single Close button
- `ILogger<HelloWorldViewModel>` logs each greeting at `Information` level
- Demonstrates: two-way binding, async commands, `ContentDialog`, DI-injected logging

---

## [A] Global Exception Handler

Registered in `App.axaml.cs` before the DI host is built, so crashes during startup are caught:

```csharp
// App.axaml.cs — registered before host.Build()
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    var ex = e.ExceptionObject as Exception;
    // Use NLog directly here — DI host may not be ready
    LogManager.GetCurrentClassLogger().Fatal(ex, "Unhandled domain exception");
    // On Desktop: show error dialog then exit
};

TaskScheduler.UnobservedTaskException += (_, e) =>
{
    _logger?.LogError(e.Exception, "Unobserved task exception");
    e.SetObserved(); // prevent process crash on unobserved async failures
};
```

- **Desktop:** logs to file via NLog, then shows a simple Avalonia error dialog before process exits
- **WASM:** file target is disabled; exception written to browser console via NLog
- After DI host is ready, `ILogger<App>` is used; before that, direct `LogManager` fallback

---

## [C] App Settings Persistence

**Model:**
```csharp
public class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Auto;
    // Add per-app prefs here when forking the template
}
```

**Service interface:**
```csharp
public interface ISettingsService
{
    AppSettings Current { get; }
    Task LoadAsync();
    Task SaveAsync();
}
```

**Storage — IsolatedStorage (unified for Desktop + WASM):**
```csharp
// SettingsService.cs
using var store = IsolatedStorageFile.GetUserStoreForApplication();
using var stream = store.OpenFile("settings.json", FileMode.OpenOrCreate);
// Serialize/deserialize AppSettings via System.Text.Json
```

- Same code path on both targets — no `#if BROWSER` needed for settings
- `LoadAsync()` called at startup in `App.axaml.cs` before `MainWindow` is shown
- `SaveAsync()` called on every setting change; errors swallowed and logged (non-fatal)
- File name: `settings.json`

---

## [G] Window State Persistence

**Desktop only** — WASM target skips this service registration entirely.

**Model:**
```csharp
public class SavedWindowState
{
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public double? X { get; set; } = null;      // null = let OS place window
    public double? Y { get; set; } = null;
    public bool IsMaximized { get; set; } = false;
}
```

**Service:**
```csharp
public interface IWindowStateService
{
    Task RestoreAsync(Window window);
    Task SaveAsync(Window window);
}
```

**Storage:** `windowstate.json` in the same `IsolatedStorage` store as `settings.json`

**Lifecycle:**
- `RestoreAsync()` called in `MainWindow.OnOpened`
- `SaveAsync()` called in `MainWindow.OnClosing`
- Off-screen guard: if restored position falls outside all current screen bounds, position is reset to null (OS placement) to handle disconnected monitors

---

## Logging Setup

```
Microsoft.Extensions.Logging    abstraction — all VMs and services depend on this only
NLog.Extensions.Logging         provider — registered once, transparent to callers
```

**nlog.config target — File only:**
```xml
<target name="file" xsi:type="File"
        fileName="logs/${shortdate}.log"
        layout="${longdate} [${level:uppercase=true}] ${logger:shortName=true} — ${message} ${exception:format=tostring}"
        archiveEvery="Day"
        maxArchiveFiles="30" />
```

- WASM: `nlog.config` loaded at runtime with file target disabled via a `<when condition="'${environment:BROWSER}'=='true'" action="Discard" />` filter or compile-time exclusion
- No console or debugger targets in file (file-only per decision 7)

**DI registration:**
```csharp
host.Services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddNLog("nlog.config");
});
```

**Structured startup log:**
```
2026-05-04 10:00:00 [INFO] App — Starting | Name=AppTemplate Version=1.0.0 Platform=Desktop OS=Windows/11
```

All ViewModels and Services receive `ILogger<T>` via constructor injection — no static `LogManager` calls in application code outside of the pre-host exception handler fallback.

---

## Deferred Suggestions (not in this template)

| # | Suggestion | When to add |
|---|---|---|
| B | Unit test project (xUnit + NSubstitute) | After template structure is stable |
| D | Localization scaffold (ResX + IStringLocalizer) | When a concrete app needs it |
| E | GitHub Actions CI | When a repo is created for a derived app |
| F | Error/404 navigation page | When WASM deep-link routing is needed |

---

## Implementation Order

1. Scaffold solution: `AppTemplate` shared project + `AppTemplate.Desktop` + `AppTemplate.Browser`
2. Install and verify all NuGet packages; confirm build on both targets
3. `App.axaml.cs`: DI host skeleton + global exception handlers [A]
4. `nlog.config` + logging registration; verify log file is created on Desktop run
5. `AppSettings` model + `ISettingsService` + `SettingsService` (IsolatedStorage) [C]
6. `IThemeService` + `ThemeService`; apply persisted theme on startup
7. `ViewModelBase` + `PageViewModelBase`; `ViewLocator`; `DataTemplates` in `App.axaml`
8. `INavigationService` + `NavigationService`
9. `MainView.axaml` NavigationView shell + `MainViewModel` (nav items, pane toggle)
10. `HomeView` + `HomeViewModel`
11. `HelloWorldView` + `HelloWorldViewModel` + `ContentDialog` greeting
12. `SettingsView` + `SettingsViewModel` (theme switcher, version display)
13. `SavedWindowState` model + `IWindowStateService` + `WindowStateService`; wire to `MainWindow` [G]
14. Smoke-test Desktop (`dotnet run`) and WASM Browser (`dotnet run --project AppTemplate.Browser`) targets
15. Final cleanup: remove unused usings, verify no static logger calls, update README

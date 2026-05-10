# AppTemplate — Avalonia 12 + FluentAvalonia Application Template

A production-ready, reusable starter template for cross-platform desktop and WebAssembly applications built with Avalonia 12 and FluentAvaloniaUI 3.

**Author:** Matthew Wormington  
**Version:** 1.0.0 — 4 May 2026  
**License:** MIT — see [LICENSE](LICENSE)

---

## AI Disclosure

This project was developed using **vibe coding** with [Claude Code](https://claude.ai/claude-code) (Anthropic, claude-sonnet-4-6). AI assistance was used throughout: project architecture, all source code, compatibility research and debugging, and this documentation.

All code was verified to build without errors and confirmed to render correctly at runtime on both Desktop and Browser (WASM) targets by the author. The author takes full responsibility for the published content.

> This disclosure follows the transparency recommendations of the Nature Methods editorial *"Using AI responsibly in scientific publishing"* (Nature Methods 23, 271, 2026; https://doi.org/10.1038/s41592-026-03020-1), which requires disclosure of AI tools used, the model/version, the scope of use, and confirmation that all AI-assisted content has been validated by the author.

---

## Features

- **Collapsible NavigationView shell** — left pane with hamburger toggle, powered by `FANavigationView`
- **Three pages** — Home, HelloWorld (ContentDialog demo), Settings
- **Light / Dark / Auto theme switching** — persisted across sessions
- **MVVM navigation** — ViewModel-switching pattern, no Frame, CommunityToolkit.Mvvm source generators
- **Dependency injection** — `Microsoft.Extensions.DependencyInjection` wired in `App.axaml.cs`
- **Structured logging** — `Microsoft.Extensions.Logging` abstraction + NLog file provider
- **App settings persistence** — `IsolatedStorage` + `System.Text.Json` (same code path on Desktop and WASM)
- **Window state persistence** — size, position, maximized state saved and restored (Desktop only)
- **Global exception handler** — `AppDomain` + `TaskScheduler` hooks, crash logged before process exits

---

## Technology Stack

| Layer | Package | Version |
|---|---|---|
| Runtime | .NET 10, C# 13 | 10.x |
| UI Framework | Avalonia | **12.0.0** (pinned — see [version notes](#version-compatibility)) |
| Fluent Controls | FluentAvaloniaUI | **3.0.0-preview2** |
| Icons | FluentIcons.Avalonia.Fluent | 2.0.325 |
| MVVM | CommunityToolkit.Mvvm | 8.4.0 |
| DI Container | Microsoft.Extensions.DependencyInjection | 10.0.7 |
| Logging | Microsoft.Extensions.Logging + NLog + NLog.Extensions.Logging | 10.0.7 / 6.1.3 |
| Settings Storage | System.IO.IsolatedStorage + System.Text.Json | built-in |

---

## Getting Started

**Prerequisites:** .NET 10 SDK, Git

**Clone / copy this template** then:

```bash
# Run on Desktop
dotnet run --project AppTemplate.Desktop/AppTemplate.Desktop.csproj

# Run in browser (WASM)
dotnet run --project AppTemplate.Browser/AppTemplate.Browser.csproj
# Open http://localhost:5235/
```

---

## Project Structure

```
AppTemplate/
├── AppTemplate.sln
├── Directory.Packages.props              ← Central NuGet version management
├── AppTemplate/                          ← Shared project (namespace: MyApp)
│   ├── Assets/
│   │   ├── avalonia-logo.ico
│   │   └── nlog.config                  ← File-only logging, 30-day rolling archive
│   ├── Models/
│   │   ├── AppSettings.cs               ← Theme enum + serializable prefs
│   │   └── SavedWindowState.cs          ← Window bounds model
│   ├── ViewModels/
│   │   ├── Base/
│   │   │   ├── ViewModelBase.cs         ← ObservableObject
│   │   │   └── PageViewModelBase.cs     ← Adds Title property
│   │   ├── MainViewModel.cs             ← Nav state, IsPaneOpen, CurrentPage
│   │   ├── HomeViewModel.cs
│   │   ├── HelloWorldViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainView.axaml               ← FANavigationView shell
│   │   ├── HomeView.axaml
│   │   ├── HelloWorldView.axaml
│   │   └── SettingsView.axaml
│   ├── Services/
│   │   ├── INavigationService / NavigationService
│   │   ├── IThemeService / ThemeService
│   │   ├── ISettingsService / SettingsService
│   │   └── IWindowStateService / WindowStateService
│   ├── App.axaml                        ← DataTemplates, FluentAvaloniaTheme
│   ├── App.axaml.cs                     ← DI host + exception handlers
│   └── ViewLocator.cs                   ← Convention-based VM→View mapping
├── AppTemplate.Desktop/
└── AppTemplate.Browser/
```

---

## Architecture

### MVVM Navigation (no Frame)

Navigation is ViewModel-switching — the recommended pattern for Avalonia MVVM apps:

```csharp
// INavigationService fires a Navigated event
public interface INavigationService
{
    event Action<PageViewModelBase>? Navigated;
    void Navigate<TPage>() where TPage : PageViewModelBase;
}

// MainViewModel subscribes and exposes CurrentPage
nav.Navigated += vm => CurrentPage = vm;

// ContentControl in MainView.axaml resolves the view via DataTemplates
<ContentControl Content="{Binding CurrentPage}" />
```

`ViewLocator` maps `HomeViewModel` → `HomeView` by naming convention. `DataTemplates` in `App.axaml` register each mapping. All page ViewModels are singletons — state is preserved across navigation.

### Dependency Injection

The DI host is built in `App.axaml.cs` before the first window is shown:

```csharp
var sc = new ServiceCollection();
sc.AddLogging(b => { b.ClearProviders(); b.AddNLog(); });
sc.AddSingleton<INavigationService, NavigationService>();
sc.AddSingleton<ISettingsService, SettingsService>();
sc.AddSingleton<IThemeService, ThemeService>();
sc.AddSingleton<IWindowStateService, WindowStateService>();
sc.AddSingleton<MainViewModel>();
sc.AddSingleton<HomeViewModel>();
// ...
_services = sc.BuildServiceProvider();
```

### Settings and Window State Persistence

Both use `IsolatedStorage` — the same code path runs on Desktop and WASM without `#if BROWSER` guards:

```csharp
using var store = IsolatedStorageFile.GetUserStoreForApplication();
using var stream = store.OpenFile("settings.json", FileMode.OpenOrCreate);
var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream);
```

Window state is skipped on WASM via `OperatingSystem.IsBrowser()` guard.

### Theme Service

```csharp
Application.Current.RequestedThemeVariant = theme switch
{
    AppTheme.Light => ThemeVariant.Light,
    AppTheme.Dark  => ThemeVariant.Dark,
    _              => ThemeVariant.Default   // follows OS
};
```

Theme is loaded from settings and applied before the first render to prevent flash.

---

## Version Compatibility

> **Critical:** Do not upgrade Avalonia or FluentAvaloniaUI without reading this section.

### FA Version Chain

| FluentAvaloniaUI | Targets Avalonia | Notes |
|---|---|---|
| 2.5.x | 11.3.12 | FA 2.x — control names without prefix |
| 3.0.0-preview2 | **12.0.0 only** | FA 3.x — all controls renamed with `FA` prefix |

### Why Avalonia is pinned to 12.0.0

FA 3.0-preview2 internally references `Avalonia.Controls.Chrome.TitleBar`, which was reorganized between Avalonia 12.0.0 and 12.0.2. Running FA 3.0-preview2 against Avalonia 12.0.2 causes a runtime `MissingMemberException` at startup.

**Do not change this line in `Directory.Packages.props`:**
```xml
<PackageVersion Include="Avalonia" Version="12.0.0" />
```

When FA releases a stable 3.x build that pins against a later Avalonia, update both versions together.

### FA 3.0 API Changes (from 2.x)

All FluentAvalonia controls gained the `FA` prefix in v3:

| v2 (Avalonia 11) | v3 (Avalonia 12) |
|---|---|
| `NavigationView` | `FANavigationView` |
| `NavigationViewItem` | `FANavigationViewItem` |
| `NavigationViewSelectionChangedEventArgs` | `FANavigationViewSelectionChangedEventArgs` |
| `SymbolIconSource` | `FASymbolIconSource` |
| `Symbol` | `FASymbol` |
| `ContentDialog` | `FAContentDialog` |

XAML namespace: `xmlns:ui="using:FluentAvalonia.UI.Controls"`

---

## Known XAML Workarounds

These patterns look unusual but are intentional — see the session summary for full diagnosis.

### FluentAvaloniaTheme — class instance, not StyleInclude

`StyleInclude` URIs do not work for this package. Instantiate the class directly:

```xml
<!-- App.axaml -->
<Application.Styles>
    <fav:FluentAvaloniaTheme xmlns:fav="using:FluentAvalonia.Styling" />
</Application.Styles>
```

### Icons set in code-behind, not XAML

The XAML compiler cannot resolve `FASymbolIconSource` as a property value inside `FANavigationViewItem`. Set icons in the `Loaded` handler instead:

```csharp
private void OnLoaded(object? sender, RoutedEventArgs e)
{
    NavHome.IconSource       = new FASymbolIconSource { Symbol = FASymbol.Home };
    NavHelloWorld.IconSource = new FASymbolIconSource { Symbol = FASymbol.People };
    NavSettings.IconSource   = new FASymbolIconSource { Symbol = FASymbol.Settings };
}
```

### SelectionChanged subscribed in code-behind

Avalonia's compiled XAML treats `SelectionChanged="Handler"` as a property assignment, not event wiring. Subscribe in code-behind:

```csharp
NavView.SelectionChanged += OnNavSelectionChanged;
```

---

## Extending the Template

### Adding a New Page

1. Create `ViewModels/MyPageViewModel.cs` extending `PageViewModelBase`
2. Create `Views/MyPageView.axaml` (UserControl)
3. Register as singleton in `App.axaml.cs` → `ConfigureServices`
4. Add a `DataTemplate` entry in `App.axaml`
5. Add a `FANavigationViewItem` in `MainView.axaml` with a unique `Tag`
6. Add the tag case to `MainViewModel.NavigateTo()`

### Adding Settings Fields

Add properties to `AppSettings.cs`. `SettingsService` serializes the whole object — new fields with default values are backward-compatible. Expose them on `SettingsViewModel` and bind in `SettingsView.axaml`.

### Logging

All ViewModels and Services receive `ILogger<T>` via constructor injection. No static logger calls needed:

```csharp
public class MyViewModel(ILogger<MyViewModel> logger) : PageViewModelBase
{
    [RelayCommand]
    private void DoSomething() => logger.LogInformation("Did something");
}
```

Log files: `AppTemplate.Desktop/bin/Debug/net10.0/logs/YYYY-MM-DD.log`

---

## References

### Avalonia

- **Official Docs** — https://docs.avaloniaui.net/
- **Avalonia GitHub** — https://github.com/AvaloniaUI/Avalonia
- **Release Notes / Changelog** — https://github.com/AvaloniaUI/Avalonia/releases
- **Upgrading to Avalonia 12** — https://docs.avaloniaui.net/docs/next/stay-up-to-date/breaking-changes
- **XAML Compiled Bindings** — https://docs.avaloniaui.net/docs/basics/data/data-binding/compiled-bindings
- **IsolatedStorage on WASM** — supported via the browser's IndexedDB backend; same API as Desktop

### FluentAvalonia

- **FluentAvaloniaUI GitHub** — https://github.com/amwx/FluentAvalonia
- **FA v3 Migration Notes** — See the GitHub releases page; v3 targets Avalonia 12 and renames all controls with the `FA` prefix
- **FA Controls Gallery** (v2, Avalonia 11) — https://avaloniaui.github.io/FluentAvaloniaSamples/ *(reference only — v3 API differs)*
- **FA NuGet** — https://www.nuget.org/packages/FluentAvaloniaUI

### Learning Resources

- **The Avalonia Book** (Wiesław Soltes) — https://wieslawsoltes.github.io/AvaloniaBook/  
  Comprehensive free online book covering Avalonia architecture, controls, MVVM patterns, styling, and cross-platform considerations. Highly recommended for anyone new to Avalonia or migrating from WPF/WinUI.

- **CommunityToolkit.Mvvm Docs** — https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
- **NLog Configuration Reference** — https://nlog-project.org/config/
- **Microsoft.Extensions.DependencyInjection** — https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection

---

## Session History

A full record of the build session — all errors encountered, fixes applied, and architectural decisions — is saved at:

```
C:\Matthew\Claude\AvaloniaTemplate_ChatSummary.md
```

The original implementation plan is at:

```
C:\Matthew\Claude\AvaloniaTemplate_Plan.md
```

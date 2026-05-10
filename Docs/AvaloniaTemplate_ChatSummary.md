# Avalonia Template — Build Session Summary

**Date:** 2026-05-04  
**Session:** Full template build from plan through Desktop smoke test and WASM launch

---

## What Was Built

A reusable Avalonia 12 + FluentAvalonia 3 application template at:

```
C:\Matthew\Claude\AppTemplate\
```

Full solution with three projects:
- `AppTemplate` — shared Avalonia project (namespace `MyApp`)
- `AppTemplate.Desktop` — Desktop entry point
- `AppTemplate.Browser` — WebAssembly entry point

---

## Decisions Made (Q&A)

| # | Question | Answer |
|---|---|---|
| 1 | MVVM toolkit | CommunityToolkit.Mvvm |
| 2 | Settings persistence | IsolatedStorage (unified Desktop + WASM) |
| 3 | WASM hosting | Localhost dev only |
| 4 | HelloWorld dialog | FluentAvalonia `FAContentDialog` |
| 5 | Project / namespace | `AppTemplate` / `MyApp` |
| 6 | Extra pages | None — Home + HelloWorld sufficient |
| 7 | NLog targets | File only |
| 8 | Desktop packaging | Framework-dependent (`dotnet run`) |
| 9 | Included suggestions | A (global exception handler), C (settings persistence), G (window state) |

---

## Final Technology Stack

| Layer | Package | Version |
|---|---|---|
| Runtime | .NET 10, C# 13 | 10.x |
| UI Framework | Avalonia | **12.0.0** (pinned — see version notes below) |
| Fluent Controls | FluentAvaloniaUI | **3.0.0-preview2** |
| Icons | FluentIcons.Avalonia.Fluent | 2.0.325 |
| MVVM | CommunityToolkit.Mvvm | 8.4.0 |
| DI Container | Microsoft.Extensions.DependencyInjection | 10.0.7 |
| Logging Abstraction | Microsoft.Extensions.Logging | 10.0.7 |
| Logging Provider | NLog + NLog.Extensions.Logging | 6.1.3 |
| Settings Storage | System.IO.IsolatedStorage + System.Text.Json | built-in |

---

## Errors Encountered and Fixed

### 1. Missing `<ImplicitUsings>enable</ImplicitUsings>`
All `System.*` types (Task, Action, EventHandler, IServiceProvider) were unresolved.  
**Fix:** Added `<ImplicitUsings>enable</ImplicitUsings>` to `AppTemplate.csproj`.

### 2. `BindingPlugins.DataValidators` inaccessible
Avalonia 12 internalized this API.  
**Fix:** Removed the line entirely.

### 3. Wrong FluentAvalonia StyleInclude URI
Using `avares://FluentAvalonia/Styling/StylesV2.axaml` (the Avalonia 11 pattern) failed silently.  
**Fix:** Instantiate the class directly in XAML — `StyleInclude` does not work for this package:
```xml
<Application.Styles>
    <fav:FluentAvaloniaTheme xmlns:fav="using:FluentAvalonia.Styling" />
</Application.Styles>
```

### 4. `NavigationViewItem.Icon` unresolvable in XAML
FA's NavigationViewItem uses `IconSource`, not `Icon`. XAML compiler couldn't resolve it.  
**Fix:** Remove icon XAML entirely; set icons in code-behind `Loaded` handler:
```csharp
NavHome.IconSource = new FASymbolIconSource { Symbol = FASymbol.Home };
```

### 5. `SelectionChanged` event wiring failed in XAML
Avalonia compiled XAML treated `SelectionChanged` as a property attribute, not an event.  
**Fix:** Subscribe in code-behind:
```csharp
NavView.SelectionChanged += OnNavSelectionChanged;
```

### 6. `TextBox.Watermark` obsolete
**Fix:** Changed to `PlaceholderText`.

### 7. FA 2.5.1 runtime crash — `Avalonia.Controls.Chrome.TitleBar` not found
FA 2.5.1 targets Avalonia 11.3.12. `Chrome.TitleBar` does not exist in Avalonia 12.  
**Fix:** Upgrade to FluentAvaloniaUI **3.0.0-preview2** (targets Avalonia 12 + .NET 10).

### 8. FA 3.0 — all control types renamed with `FA` prefix
`NavigationView` → `FANavigationView`, `ContentDialog` → `FAContentDialog`, `Symbol` → `FASymbol`, etc.  
**Fix:** Update all usages in XAML and code-behind.

### 9. FA 3.0-preview2 still crashed with Avalonia 12.0.2
FA 3.0-preview2 internally references `Avalonia.Controls.Chrome.TitleBar`, which was reorganized between Avalonia 12.0.0 and 12.0.2.  
**Fix:** Pin Avalonia to exactly `12.0.0` in `Directory.Packages.props`. Do not upgrade to 12.0.2.

### 10. WASM port conflict (7169 already in use)
A prior backgrounded `dotnet run` was still occupying the port.  
**Fix:** `Stop-Process -Id <pid> -Force` then restart.

---

## Key Patterns Established

### FluentAvaloniaTheme — class instantiation, not StyleInclude
```xml
<Application.Styles>
    <fav:FluentAvaloniaTheme xmlns:fav="using:FluentAvalonia.Styling" />
</Application.Styles>
```

### Icons in code-behind (not XAML)
```csharp
NavHome.IconSource = new FASymbolIconSource { Symbol = FASymbol.Home };
```

### NavigationView SelectionChanged in code-behind
```csharp
NavView.SelectionChanged += OnNavSelectionChanged;
private void OnNavSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs args)
{
    var tag = (args.SelectedItem as FANavigationViewItem)?.Tag?.ToString();
    vm.NavigateTo(tag);
}
```

### IsolatedStorage — unified Desktop + WASM settings
```csharp
using var store = IsolatedStorageFile.GetUserStoreForApplication();
using var stream = store.OpenFile("settings.json", FileMode.OpenOrCreate);
```

### WASM guard in WindowStateService
```csharp
public async Task RestoreAsync(Window window)
{
    if (OperatingSystem.IsBrowser()) return;
    // ...
}
```

### ViewModel-switching navigation (no Frame)
```csharp
// INavigationService fires Navigated event
// MainViewModel subscribes: nav.Navigated += vm => CurrentPage = vm;
// ViewLocator maps HomeViewModel → HomeView by naming convention
```

---

## Running the Template

**Desktop:**
```
cd C:\Matthew\Claude\AppTemplate
dotnet run --project AppTemplate.Desktop/AppTemplate.Desktop.csproj
```

**WASM (browser):**
```
dotnet run --project AppTemplate.Browser/AppTemplate.Browser.csproj
# Open http://localhost:5235/ or https://localhost:7169/
```

---

## Files Created

```
AppTemplate/
├── AppTemplate.sln
├── Directory.Packages.props                  ← Central package management
├── AppTemplate/
│   ├── AppTemplate.csproj
│   ├── Assets/
│   │   ├── avalonia-logo.ico
│   │   └── nlog.config
│   ├── Models/
│   │   ├── AppSettings.cs
│   │   └── SavedWindowState.cs
│   ├── ViewModels/
│   │   ├── Base/
│   │   │   ├── ViewModelBase.cs
│   │   │   └── PageViewModelBase.cs
│   │   ├── MainViewModel.cs
│   │   ├── HomeViewModel.cs
│   │   ├── HelloWorldViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainView.axaml / .axaml.cs
│   │   ├── HomeView.axaml / .axaml.cs
│   │   ├── HelloWorldView.axaml / .axaml.cs
│   │   └── SettingsView.axaml / .axaml.cs
│   ├── Services/
│   │   ├── INavigationService.cs / NavigationService.cs
│   │   ├── IThemeService.cs / ThemeService.cs
│   │   ├── ISettingsService.cs / SettingsService.cs
│   │   └── IWindowStateService.cs / WindowStateService.cs
│   ├── App.axaml / App.axaml.cs
│   └── ViewLocator.cs
├── AppTemplate.Desktop/
│   ├── Program.cs
│   └── AppTemplate.Desktop.csproj
└── AppTemplate.Browser/
    ├── Program.cs
    └── AppTemplate.Browser.csproj
```

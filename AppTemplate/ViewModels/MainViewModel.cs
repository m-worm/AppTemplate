using AppTemplate.Services;
using AppTemplate.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace AppTemplate.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _nav;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private PageViewModelBase? _currentPage;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    public MainViewModel(INavigationService nav, ILogger<MainViewModel> logger)
    {
        _nav = nav;
        _logger = logger;
        nav.Navigated += vm => CurrentPage = vm;
        nav.Navigate<HomeViewModel>();
    }

    public void NavigateTo(string? tag)
    {
        switch (tag)
        {
            case "Home":       _nav.Navigate<HomeViewModel>();       break;
            case "HelloWorld": _nav.Navigate<HelloWorldViewModel>(); break;
            case "Settings":   _nav.Navigate<SettingsViewModel>();   break;
            default:
                _logger.LogWarning("Unknown navigation tag: {Tag}", tag);
                break;
        }
    }
}

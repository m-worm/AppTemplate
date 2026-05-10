using AppTemplate.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace AppTemplate.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _sp;

    public NavigationService(IServiceProvider sp) => _sp = sp;

    public event Action<PageViewModelBase>? Navigated;

    public void Navigate<TPage>() where TPage : PageViewModelBase
    {
        var vm = _sp.GetRequiredService<TPage>();
        Navigated?.Invoke(vm);
    }
}

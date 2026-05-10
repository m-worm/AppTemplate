using AppTemplate.ViewModels.Base;

namespace AppTemplate.Services;

public interface INavigationService
{
    event Action<PageViewModelBase>? Navigated;
    void Navigate<TPage>() where TPage : PageViewModelBase;
}

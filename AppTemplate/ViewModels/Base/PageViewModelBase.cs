using CommunityToolkit.Mvvm.ComponentModel;

namespace AppTemplate.ViewModels.Base;

public abstract partial class PageViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;
}

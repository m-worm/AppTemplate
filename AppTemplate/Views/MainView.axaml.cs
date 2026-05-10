using Avalonia.Controls;
using AppTemplate.ViewModels;
using FluentAvalonia.UI.Controls;

namespace AppTemplate.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NavHome.IconSource       = new FASymbolIconSource { Symbol = FASymbol.Home };
        NavHelloWorld.IconSource = new FASymbolIconSource { Symbol = FASymbol.People };
        NavSettings.IconSource   = new FASymbolIconSource { Symbol = FASymbol.Settings };

        NavView.SelectionChanged += OnNavSelectionChanged;
        NavView.SelectedItem = NavHome;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NavView.SelectionChanged -= OnNavSelectionChanged;
        Unloaded -= OnUnloaded;
    }

    private void OnNavSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs args)
    {
        if (DataContext is not MainViewModel vm) return;
        var tag = (args.SelectedItem as FANavigationViewItem)?.Tag?.ToString();
        vm.NavigateTo(tag);
    }
}

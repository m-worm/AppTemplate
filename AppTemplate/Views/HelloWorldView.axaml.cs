using Avalonia.Controls;
using AppTemplate.ViewModels;
using FluentAvalonia.UI.Controls;

namespace AppTemplate.Views;

public partial class HelloWorldView : UserControl
{
    private HelloWorldViewModel? _currentVm;

    public HelloWorldView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentVm != null)
            _currentVm.GreetingRequested -= OnGreetingRequested;

        _currentVm = DataContext as HelloWorldViewModel;

        if (_currentVm != null)
            _currentVm.GreetingRequested += OnGreetingRequested;
    }

    private async void OnGreetingRequested(object? sender, string greeting)
    {
        var dialog = new FAContentDialog
        {
            Title = "Hello!",
            Content = greeting,
            CloseButtonText = "OK"
        };

        await dialog.ShowAsync();
    }
}

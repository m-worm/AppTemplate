using AppTemplate.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AppTemplate.ViewModels;

public partial class HelloWorldViewModel : PageViewModelBase
{
    private readonly ILogger<HelloWorldViewModel> _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SayHelloCommand))]
    private string _name = string.Empty;

    public event EventHandler<string>? GreetingRequested;

    public HelloWorldViewModel(ILogger<HelloWorldViewModel> logger)
    {
        _logger = logger;
        Title = "Hello World";
    }

    [RelayCommand(CanExecute = nameof(CanSayHello))]
    private void SayHello()
    {
        _logger.LogInformation("Greeting {Name}", Name);
        GreetingRequested?.Invoke(this, $"Hello \"{Name}\"!");
    }

    private bool CanSayHello() => !string.IsNullOrWhiteSpace(Name);
}

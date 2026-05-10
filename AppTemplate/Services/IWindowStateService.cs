using Avalonia.Controls;

namespace AppTemplate.Services;

public interface IWindowStateService
{
    Task RestoreAsync(Window window);
    Task SaveAsync(Window window);
}

using AppTemplate.Models;

namespace AppTemplate.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    Task LoadAsync();
    Task SaveAsync();
}

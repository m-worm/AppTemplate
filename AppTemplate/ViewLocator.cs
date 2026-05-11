using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AppTemplate.ViewModels;
using AppTemplate.ViewModels.Base;
using AppTemplate.Views;

namespace AppTemplate;

public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> _views = new()
    {
        { typeof(HomeViewModel),       () => new HomeView() },
        { typeof(HelloWorldViewModel), () => new HelloWorldView() },
        { typeof(SettingsViewModel),   () => new SettingsView() },
    };

    public Control? Build(object? param)
    {
        if (param is null) return null;

        if (_views.TryGetValue(param.GetType(), out var factory))
            return factory();

        return new TextBlock { Text = "View not found: " + param.GetType().FullName };
    }

    public bool Match(object? data) => data is PageViewModelBase;
}

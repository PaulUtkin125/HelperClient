using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using HelperClient.Pages;
using HelperClient.ViewModels;

namespace HelperClient
{
    /// <summary>
    /// Сопоставление ViewModel → View для ContentControl в MainWindow.
    /// </summary>
    public class ViewLocator : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public Control? Build(object? data)
        {
            if (data is HomeViewModel) return new Home();
            if (data is UpdateViewModel) return new Update();
            return new TextBlock { Text = "Не найдено представление для: " + data?.GetType().Name };
        }

        public bool Match(object? data) =>
            data is HomeViewModel or UpdateViewModel;
    }
}

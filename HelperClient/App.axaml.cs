using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HelperClient.ViewModels;

namespace HelperClient
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Фоновая инициализация композиции сервисов клиента (API, профиль, мониторинг сети).
            ViewModelLocator.Initialize();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.Exit += (_, _) => ViewModelLocator.Shutdown();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
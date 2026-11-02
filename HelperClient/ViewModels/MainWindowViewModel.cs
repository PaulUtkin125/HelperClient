using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HelperClient.ViewModels
{
    /// <summary>
    /// Корневая ViewModel окна. Сервисы клиента инициализируются асинхронно
    /// (ViewModelLocator), страницы создаются после их готовности.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? _currentPage;

        [ObservableProperty]
        private string _pageHeadTitle = "С какой проблемой Вы столкнулись?";

        private HomeViewModel? _homeViewModel;
        private MenuViewModel? _menuViewModel;
        private PartyLicensyViewModel? _partyLicensyViewModel;

        public MainWindowViewModel()
        {
            ViewModelLocator.ServicesReady += OnServicesReady;
            if (ViewModelLocator.IsReady) InitializePages();
        }

        private void OnServicesReady(object? sender, EventArgs e) =>
            Dispatcher.UIThread.Post(InitializePages);

        private void InitializePages()
        {
            if (_homeViewModel is not null) return;
            try
            {
                var services = ViewModelLocator.Current;
                _homeViewModel = new HomeViewModel(services);
                _menuViewModel = new MenuViewModel(this);
                _partyLicensyViewModel = new PartyLicensyViewModel();
                CurrentPage = _homeViewModel;
            }
            catch (Exception ex)
            {
                CurrentPage = new StartupErrorViewModel($"Не удалось инициализировать клиент: {ex.Message}");
            }
        }

        [RelayCommand]
        private void GoToMenu()
        {
            // До готовности сервисов меню недоступно.
            if (_menuViewModel is null || _homeViewModel is null) return;

            if (CurrentPage is MenuViewModel)
            {
                PageHeadTitle = "С какой проблемой Вы столкнулись?";
                CurrentPage = _homeViewModel;
            }
            else
            {
                PageHeadTitle = "Меню";
                CurrentPage = _menuViewModel;
            }
        }
    }

    /// <summary>Показывается, если композиция сервисов не удалась.</summary>
    public sealed class StartupErrorViewModel
    {
        public string Message { get; }

        public StartupErrorViewModel(string message) => Message = message;
    }
}

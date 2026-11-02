using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperClient.Services;

namespace HelperClient.ViewModels
{
    /// <summary>Корневая модель окна: страницы Home/Update + команда открытия авторизации.</summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        public HomeViewModel Home { get; }
        public UpdateViewModel Update { get; }
        public AuthService Auth { get; }

        /// <summary>Открывает окно входа (вызывается из code-behind MainWindow).</summary>
        public event Action? OpenAuthRequested;

        public MainWindowViewModel(HomeViewModel home, UpdateViewModel update, AuthService auth, ApiClient api)
        {
            Home = home;
            Update = update;
            Auth = auth;

            // токен истёк/невалиден (401) — предлагаем войти заново
            api.Unauthorized += () =>
            {
                Home.IsAuthenticated = false;
                OpenAuthRequested?.Invoke();
            };
        }

        [RelayCommand]
        private void OpenAuth() => OpenAuthRequested?.Invoke();

        /// <summary>После успешного входа обновляем состояние страниц.</summary>
        public void OnAuthorizationSuccess()
        {
            Home.IsAuthenticated = true;
            _ = Home.RefreshCategories();
            _ = Update.Refresh();
        }
    }
}

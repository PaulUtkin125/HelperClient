using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperClient.Services;

namespace HelperClient.ViewModels
{
    /// <summary>Модель окна авторизации (POST /HelperAPI/Authorize/Login).</summary>
    public partial class AuthViewModel : ObservableObject
    {
        private readonly AuthService _auth;

        [ObservableProperty]
        private string _login = "";

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _errorMessage;

        /// <summary>Истины, когда пользователь уже имеет сохранённый токен.</summary>
        public bool IsAlreadyAuthenticated => _auth.IsAuthenticated;

        public event Action? AuthorizationSuccess;

        public AuthViewModel(AuthService auth)
        {
            _auth = auth;
        }

        partial void OnLoginChanged(string value) => ErrorMessage = null;
        partial void OnPasswordChanged(string value) => ErrorMessage = null;

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(Login))
            {
                ErrorMessage = "Введите имя пользователя.";
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Введите пароль.";
                return;
            }

            IsBusy = true;
            LoginCommand.NotifyCanExecuteChanged();
            try
            {
                var result = await _auth.LoginAsync(Login, Password);
                if (result)
                    AuthorizationSuccess?.Invoke();
                else
                    ErrorMessage = "Сервер не вернул токен авторизации.";
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                ErrorMessage = "Неверное имя пользователя или пароль.";
            }
            catch (ApiException ex) when (ex.StatusCode == 400)
            {
                ErrorMessage = ex.Message ?? "Некорректные данные формы.";
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                LoginCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanLogin() => !IsBusy;
    }
}

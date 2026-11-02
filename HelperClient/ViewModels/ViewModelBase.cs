using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HelperClient.Services;

namespace HelperClient.ViewModels
{
    /// <summary>
    /// Базовая ViewModel: общий статусный текст и безопасное выполнение команд.
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        protected ClientServiceRegistry Services { get; }

        protected ViewModelBase(ClientServiceRegistry services) => Services = services;

        /// <summary>Показ сообщения в нижней строке страницы.</summary>
        public void ShowStatus(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            StatusMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }

        protected async Task RunAsync(Func<Task> body, string? successMessage = null)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                await body();
                if (!string.IsNullOrWhiteSpace(successMessage)) ShowStatus(successMessage);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected Task RunAsync(Func<Task> body) => RunAsync(body, null);
    }
}

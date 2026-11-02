using System;
using System.Threading.Tasks;
using HelperClient.Services;

namespace HelperClient.ViewModels
{
    /// <summary>
    /// Лёгкий сервис-локатор: один экземпляр композиции сервисов на всё приложение.
    /// Инициализация асинхронная — UI стартует сразу, настройки подгружаются в фоне.
    /// </summary>
    public static class ViewModelLocator
    {
        private static TaskCompletionSource<ClientServiceRegistry>? _source;

        public static event EventHandler? ServicesReady;

        public static bool IsReady => _source?.Task.Status == TaskStatus.RanToCompletion;

        public static ClientServiceRegistry Current =>
            _source is not null && _source.Task.IsCompletedSuccessfully
                ? _source.Task.Result
                : throw new InvalidOperationException("Сервисы клиента ещё не инициализированы");

        /// <summary>Запуск фоновой инициализации (вызывается из App).</summary>
        public static void Initialize()
        {
            if (_source is not null) return;
            var tcs = new TaskCompletionSource<ClientServiceRegistry>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _source = tcs;

            _ = Task.Run(async () =>
            {
                try
                {
                    var registry = await ClientServiceRegistry.CreateAsync().ConfigureAwait(false);
                    tcs.SetResult(registry);
                    ServicesReady?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
        }

        /// <summary>Дождаться готовности сервисов (для ViewModel).</summary>
        public static async Task<ClientServiceRegistry> GetServicesAsync()
        {
            if (_source is null) Initialize();
            return await _source!.Task.ConfigureAwait(false);
        }

        public static void Shutdown()
        {
            if (_source?.Task.IsCompletedSuccessfully == true)
            {
                _source.Task.Result.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    }
}

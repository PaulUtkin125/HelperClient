using System;
using System.Threading;
using System.Threading.Tasks;
using HelperClient.Models.DTO.MachinDto;

namespace HelperClient.Services.Background
{
    /// <summary>
    /// Фоновая задача клиента: периодически отправляет PATCH /Machine/Heartbeat
    /// с MAC-адресом текущего устройства, чтобы сервер считал IP активным
    /// (см. серверный InactiveIpsCleanupService — неактивные IP удаляются).
    /// Также выполняет POST /Machine/Sync для актуализации данных об устройстве.
    /// </summary>
    public class HeartbeatBackgroundService : IDisposable
    {
        private readonly MachineService _machines;
        private readonly MachineSyncDto _device;
        private Timer? _timer;

        /// <summary>Интервал отправки heartbeat. По умолчанию — 15 минут.</summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

        public event Action<Exception>? Failed;

        public HeartbeatBackgroundService(MachineService machines, MachineSyncDto device)
        {
            _machines = machines;
            _device = device;
        }

        public void Start()
        {
            Stop();
            _timer = new Timer(_ => _ = RunOnceAsync(), null, TimeSpan.Zero, Interval);
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private async Task RunOnceAsync()
        {
            try
            {
                // сначала синхронизируем данные машины, затем отмечаем активность
                await _machines.SyncAsync(_device);
                await _machines.HeartbeatAsync(_device.MAC_address);
            }
            catch (ApiException ex) when (ex.IsUnauthorized)
            {
                // Sync/Heartbeat доступны всем пользователям — ошибка означает сбой сервера/токена
                Failed?.Invoke(ex);
            }
            catch (Exception ex)
            {
                Failed?.Invoke(ex);
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}

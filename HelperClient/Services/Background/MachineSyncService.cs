using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelperClient.Models.DTO.MachinDto;
using HelperClient.Models.Profile;
using HelperClient.Services.Api;
using HelperClient.Services.Network;
using HelperClient.Services.Profile;

namespace HelperClient.Services.Background
{
    /// <summary>
    /// Координатор «жизни» клиента: следит за изменением IP-адресов и актуализует данные машины на сервере.
    /// Обмен — только через публичные (без авторизации) контракты API: Machine/Sync, Machine/Heartbeat.
    /// </summary>
    public interface IMachineSyncService : IAsyncDisposable
    {
        /// <summary>Текущий профиль (после загрузки из файла конфигурации).</summary>
        MachineProfile Profile { get; }

        /// <summary>Уведомление для UI: состояние синхронизации изменилось.</summary>
        event EventHandler<string>? StatusChanged;

        Task StartAsync(CancellationToken ct = default);

        Task StopAsync();

        /// <summary>Принудительная синхронизация профиля с сервером (после сохранения пользователем).</summary>
        Task<bool> SyncNowAsync(CancellationToken ct = default);

        /// <summary>Заменить профиль извне (после редактирования пользователем на странице профиля).</summary>
        void ApplyExternalProfile(MachineProfile profile);

        /// <summary>Отправить heartbeat вручную.</summary>
        Task<bool> HeartbeatNowAsync(CancellationToken ct = default);
    }

    public sealed class MachineSyncService : IMachineSyncService
    {
        private readonly IHelperApiClient _api;
        private readonly IProfileStore _store;
        private readonly INetworkChangeNotifier _notifier;
        private readonly ApiSettings _settings;

        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private CancellationTokenSource? _cts;
        private Task? _pollLoop;
        private Task? _heartbeatLoop;
        private DateTime _lastFailedSyncUtc = DateTime.MinValue;
        private bool _syncPending;
        private bool _disposed;

        /// <summary>Снимок профиля, успешно отправленный на сервер (для проверки «неотправленных» изменений).</summary>
        private MachineProfile _lastSyncedSnapshot = new();

        public MachineProfile Profile { get; private set; } = new();

        public event EventHandler<string>? StatusChanged;

        public MachineSyncService(
            IHelperApiClient api,
            IProfileStore store,
            INetworkChangeNotifier notifier,
            ApiSettings settings)
        {
            _api = api;
            _store = store;
            _notifier = notifier;
            _settings = settings;
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            Profile = await _store.LoadAsync(ct).ConfigureAwait(false);

            // Первый запуск: автоматически заполняем системную часть профиля адресами ОС.
            if (!Profile.IsComplete)
            {
                var snapshot = NetworkInfoProvider.Capture();
                if (string.IsNullOrWhiteSpace(Profile.MacAddress))
                    Profile.MacAddress = snapshot.MacAddress;
                if (Profile.IpAddresses.Count == 0)
                    Profile.IpAddresses = snapshot.Ipv4Addresses.ToList();
                if (Profile.MachineType <= 0)
                    Profile.MachineType = _settings.DefaultMachineType;

                await _store.SaveAsync(Profile, ct).ConfigureAwait(false);
            }

            _notifier.ConfigurationChanged += OnNetworkChanged;
            _notifier.Start();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _pollLoop = PollLoopAsync(_cts.Token);
            _heartbeatLoop = HeartbeatLoopAsync(_cts.Token);

            // При старте сразу пробуем актуализировать данные, если профиль полный.
            if (Profile.IsComplete)
                _ = SafeFireAndForget(SyncInternalAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            _notifier.ConfigurationChanged -= OnNetworkChanged;
            _notifier.Stop();

            if (_cts is not null)
            {
                _cts.Cancel();
                try
                {
                    if (_pollLoop is not null) await _pollLoop.ConfigureAwait(false);
                    if (_heartbeatLoop is not null) await _heartbeatLoop.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* штатное завершение */ }
            }
        }

        public async Task<bool> SyncNowAsync(CancellationToken ct = default)
        {
            if (!Profile.IsComplete)
            {
                Raise("Профиль машины не заполнен — синхронизация пропущена");
                return false;
            }

            return await SyncInternalAsync(ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void ApplyExternalProfile(MachineProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            Profile = profile.Clone();
        }

        public async Task<bool> HeartbeatNowAsync(CancellationToken ct = default)
        {
            if (!NetworkAddressValidator.IsValidMac(Profile.MacAddress)) return false;
            try
            {
                await _api.HeartbeatAsync(Profile.MacAddress, ct).ConfigureAwait(false);
                Raise("Сигнал активности отправлен");
                return true;
            }
            catch (ApiException ex) when (ex.IsNotFound)
            {
                // Машина ещё не зарегистрирована — регистрируем через Sync.
                Raise("Машина не найдена на сервере — выполняется регистрация");
                return await SyncInternalAsync(ct).ConfigureAwait(false);
            }
            catch (ApiException ex)
            {
                Raise($"Heartbeat не выполнен: {ex.Message}");
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private void OnNetworkChanged(object? sender, IReadOnlyList<string> currentIps)
        {
            if (_disposed) return;
            _ = SafeFireAndForget(HandleNetworkChangeAsync(currentIps));
        }

        private async Task HandleNetworkChangeAsync(IReadOnlyList<string> currentIps)
        {
            if (!await _syncLock.WaitAsync(TimeSpan.FromMinutes(2)).ConfigureAwait(false)) return;
            try
            {
                var changed = !Profile.IpAddresses.SequenceEqual(currentIps);
                if (!changed) return;

                Profile.IpAddresses = currentIps.ToList();
                await _store.SaveAsync(Profile).ConfigureAwait(false);
                Raise("Обнаружено изменение IP-адресов, данные актуализируются на сервере");
            }
            finally
            {
                _syncLock.Release();
            }

            // Отправка выполняется в SyncInternalAsync (он же захватит _syncLock).
            var delivered = await SyncInternalAsync().ConfigureAwait(false);
            if (!delivered)
                Raise("Сервер временно недоступен: изменения сохранены локально и будут отправлены позже");
        }

        private async Task<bool> SyncInternalAsync(CancellationToken ct = default)
        {
            if (!await _syncLock.WaitAsync(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false)) return false;
            try
            {
                var dto = BuildSyncDto();
                var response = await _api.SyncMachineAsync(dto, ct).ConfigureAwait(false);

                _lastFailedSyncUtc = DateTime.MinValue;
                _syncPending = false;
                _lastSyncedSnapshot = Profile.Clone();

                if (response is not null)
                {
                    if (response.Id > 0) Profile.ServerMachineId = response.Id;
                    Profile.ConcurrencyStamp = Math.Max(Profile.ConcurrencyStamp, response.ConcurrencyStamp);
                    await _store.SaveAsync(Profile, ct).ConfigureAwait(false);
                    Raise($"Данные машины актуализированы на сервере (ID {response.Id})");
                }
                else
                {
                    Raise("Данные машины актуализированы на сервере");
                }

                return true;
            }
            catch (ApiException ex)
            {
                _lastFailedSyncUtc = DateTime.UtcNow;
                _syncPending = true;
                Raise($"Актуализация не выполнена: {ex.Message}");
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private MachineSyncDto BuildSyncDto() => new()
        {
            MAC_address = Profile.MacAddress,
            IP_address = Profile.IpAddresses.ToList(),
            Department_id = Profile.DepartmentId,
            Housing_id = Profile.HousingId,
            MacineType = Profile.MachineType,
            Office_number = string.IsNullOrWhiteSpace(Profile.OfficeNumber) ? null : Profile.OfficeNumber!.Trim(),
            ConcurrencyStamp = Profile.ConcurrencyStamp,
        };

        private async Task PollLoopAsync(CancellationToken ct)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(15, _settings.NetworkPollSeconds));
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, ct).ConfigureAwait(false);

                    // Страховка: ОС не всегда присылает системные уведомления (например, при DHCP renew).
                    _notifier.CheckAgainst(Profile.IpAddresses);

                    // Повторная попытка доставки изменений, если прошлая синхронизация упала
                    // или профиль менялся, пока сервер был недоступен.
                    var dirty = _syncPending || !Profile.SameRegistrationData(_lastSyncedSnapshot);
                    if (_lastFailedSyncUtc != DateTime.MinValue || dirty)
                        await SyncInternalAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Raise($"Ошибка фонового мониторинга сети: {ex.Message}");
                }
            }
        }

        private async Task HeartbeatLoopAsync(CancellationToken ct)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.HeartbeatMinutes));
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, ct).ConfigureAwait(false);
                    await HeartbeatNowAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void Raise(string message) => StatusChanged?.Invoke(this, message);

        private static async Task SafeFireAndForget(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Фоновые ошибки не должны ронять приложение — они уже отражены в статусе.
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await StopAsync().ConfigureAwait(false);
            _syncLock.Dispose();
            _cts?.Dispose();
        }
    }
}

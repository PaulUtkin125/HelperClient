using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace HelperClient.Services.Network
{
    /// <summary>
    /// Подписка на системные уведомления об изменении сетевых конфигураций
    /// (NetworkInformation.NetworkAvailabilityChanged / NetworkChange),
    /// кроссплатформенно для Windows и Linux.
    /// </summary>
    public interface INetworkChangeNotifier : IDisposable
    {
        /// <summary>Событие: адресная конфигурация машины могла измениться.</summary>
        event EventHandler<IReadOnlyList<string>>? ConfigurationChanged;

        void Start();

        void Stop();
    }

    public sealed class NetworkChangeNotifier : INetworkChangeNotifier
    {
        private readonly TimeSpan _debounce;
        private readonly object _sync = new();
        private Timer? _debounceTimer;
        private volatile bool _pending;
        private bool _started;

        /// <param name="debounce">Пауза «тишины» перед уведомлением, чтобы не слать Sync на каждое мигание линка.</param>
        public NetworkChangeNotifier(TimeSpan? debounce = null) =>
            _debounce = debounce ?? TimeSpan.FromSeconds(5);

        public event EventHandler<IReadOnlyList<string>>? ConfigurationChanged;

        public void Start()
        {
            lock (_sync)
            {
                if (_started) return;
                _started = true;
                NetworkInformation.NetworkAvailabilityChanged += OnSystemEvent;
                NetworkInformation.NetworkAddressChanged += OnSystemEvent;
                _debounceTimer = new Timer(_ => FlushPending(), null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (!_started) return;
                _started = false;
                NetworkInformation.NetworkAvailabilityChanged -= OnSystemEvent;
                NetworkInformation.NetworkAddressChanged -= OnSystemEvent;
                _debounceTimer?.Dispose();
                _debounceTimer = null;
            }
        }

        private void OnSystemEvent(object? sender, EventArgs e)
        {
            _pending = true;
            lock (_sync)
            {
                _debounceTimer?.Change(_debounce, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Принудительная проверка: сравнить текущие адреса с эталоном и при расхождении вызвать событие.
        /// Используется фоновым поллером как страховка, если ОС не прислала системное уведомление.
        /// </summary>
        public bool CheckAgainst(IReadOnlyList<string> knownIps)
        {
            var current = NetworkInfoProvider.Capture().Ipv4Addresses;
            if (current.SequenceEqual(knownIps)) return false;
            Raise(current);
            return true;
        }

        /// <summary>Отправить уведомление вручную (например, после сохранения профиля).</summary>
        public void NotifyNow()
        {
            var snapshot = NetworkInfoProvider.Capture();
            Raise(snapshot.Ipv4Addresses);
        }

        private void FlushPending()
        {
            if (!_pending) return;
            _pending = false;
            var snapshot = NetworkInfoProvider.Capture();
            Raise(snapshot.Ipv4Addresses);
        }

        private void Raise(IReadOnlyList<string> ips)
        {
            try
            {
                ConfigurationChanged?.Invoke(this, ips);
            }
            catch
            {
                // Обработчик не должен ронять фоновый поток мониторинга.
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

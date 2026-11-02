using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HelperClient.Services.Network
{
    /// <summary>
    /// Снимок адресов всех активных сетевых адаптеров машины.
    /// </summary>
    public record NetworkSnapshot
    {
        /// <summary>MAC основного адаптера (первый физический активный адаптер).</summary>
        public string MacAddress { get; init; } = string.Empty;

        /// <summary>Все MAC-адреса машины (для поиска машины на сервере).</summary>
        public IReadOnlyList<string> AllMacs { get; init; } = new List<string>();

        /// <summary>IPv4/IPv6 адреса со всех сетевых адаптеров.</summary>
        public IReadOnlyList<string> IpAddresses { get; init; } = new List<string>();

        /// <summary>Только IPv4 адреса (используются при синхронизации с сервером).</summary>
        public IReadOnlyList<string> Ipv4Addresses { get; init; } = new List<string>();
    }

    /// <summary>
    /// Сбор актуальных сетевых адресов со всех адаптеров (без обращений к БД, только ОС).
    /// </summary>
    public static class NetworkInfoProvider
    {
        /// <summary>Собрать текущие адреса всех сетевых интерфейсов.</summary>
        public static NetworkSnapshot Capture()
        {
            var macs = new List<string>();
            var ipv4 = new List<string>();
            var allIps = new List<string>();
            string primaryMac = string.Empty;

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Пропускаем отключённые и служебные интерфейсы (loopback, tunnel...).
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;

                var macRaw = nic.GetPhysicalAddress().ToString();
                var mac = FormatMac(macRaw);

                if (!string.IsNullOrEmpty(mac) && mac != "00:00:00:00:00:00")
                {
                    if (!macs.Contains(mac)) macs.Add(mac);
                    if (string.IsNullOrEmpty(primaryMac) && IsPhysical(nic)) primaryMac = mac;
                }

                var props = nic.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    var address = ua.Address;
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var s = address.ToString();
                        if (!ipv4.Contains(s)) ipv4.Add(s);
                        if (!allIps.Contains(s)) allIps.Add(s);
                    }
                    else if (address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        var s = address.ToString();
                        if (!allIps.Contains(s)) allIps.Add(s);
                    }
                }
            }

            // Если физический адаптер не определён — берём первый доступный MAC.
            if (string.IsNullOrEmpty(primaryMac) && macs.Count > 0) primaryMac = macs[0];

            return new NetworkSnapshot
            {
                MacAddress = primaryMac,
                AllMacs = macs,
                IpAddresses = allIps,
                Ipv4Addresses = ipv4,
            };
        }

        private static bool IsPhysical(NetworkInterface nic) =>
            nic.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                or NetworkInterfaceType.Wireless80211
                or NetworkInterfaceType.GigabitEthernet
                or NetworkInterfaceType.FastEthernetFx;

        /// <summary>Форматирование «001122334455» → «00:11:22:33:44:55».</summary>
        public static string FormatMac(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length < 12) return string.Empty;
            raw = raw.ToUpperInvariant();
            return string.Join(":", Enumerable.Range(0, 6).Select(i => raw.Substring(i * 2, 2)));
        }
    }
}

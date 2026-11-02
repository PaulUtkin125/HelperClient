using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace HelperClient.Services.Network
{
    /// <summary>
    /// Валидация сетевых адресов по правилам спецификации (IP/MAC: валидный формат).
    /// </summary>
    public static partial class NetworkAddressValidator
    {
        // Форматы MAC, допускаемые API: 00:11:22:33:44:55, 00-11-22-33-44-55 (до 18 символов).
        private const string MacPattern =
            "^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$";

        private static readonly Regex MacRegex = MyMacRegex();

        [GeneratedRegex(MacPattern, RegexOptions.Compiled)]
        private static partial Regex MyMacRegex();

        /// <summary>Проверка корректности IPv4/IPv6 адреса.</summary>
        public static bool IsValidIp(string? value) =>
            !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value.Trim(), out _);

        /// <summary>Проверка корректности MAC-адреса.</summary>
        public static bool IsValidMac(string? value) =>
            !string.IsNullOrWhiteSpace(value) && MacRegex.IsMatch(value.Trim());

        /// <summary>Приведение MAC к каноничному виду AA:BB:CC:DD:EE:FF (верхний регистр, разделитель «:»).</summary>
        public static string NormalizeMac(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var hex = new string(value.Where(Uri.IsHexDigit).ToArray());
            if (hex.Length != 12) return value.Trim().ToUpperInvariant();
            return string.Join(":", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
        }
    }

    /// <summary>Отдельный класс для MAC, используемый моделью профиля.</summary>
    public static class MacAddressValidator
    {
        public static bool IsValid(string? value) => NetworkAddressValidator.IsValidMac(value);
    }
}

using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace HelperClient.Services
{
    /// <summary>
    /// Клиентская валидация по правилам спецификации /HelperAPI,
    /// чтобы отсекать заведомо невалидные запросы до обращения к серверу.
    /// </summary>
    public static class Validation
    {
        private static readonly Regex MacRegex =
            new Regex("^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$", RegexOptions.Compiled);

        /// <summary>name/searchString/comment: 1–255 символов, не null/пусто/только пробелы.</summary>
        public static bool IsRequiredText(string? value) =>
            !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 1 && value.Length <= 255;

        public static void EnsureRequiredText(string? value, string fieldName)
        {
            if (!IsRequiredText(value))
                throw new ArgumentException($"{fieldName}: значение должно быть непустой строкой длиной 1–255 символов.");
        }

        /// <summary>Необязательное текстовое поле: null допустим, но если задано — не пусто и ≤255.</summary>
        public static void EnsureOptionalText(string? value, string fieldName)
        {
            if (value is null) return;
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName}: пустая строка недопустима, используйте null.");
            if (value.Length > 255)
                throw new ArgumentException($"{fieldName}: длина не должна превышать 255 символов.");
        }

        public static void EnsurePositiveId(int id, string fieldName)
        {
            if (id <= 0)
                throw new ArgumentException($"{fieldName}: должен быть положительным идентификатором (id > 0).");
        }

        /// <summary>Для POST /Add справочников: id строго 0.</summary>
        public static void EnsureNewEntityId(int id)
        {
            if (id != 0)
                throw new ArgumentException("id: для новой записи значение должно быть строго 0.");
        }

        public static bool IsValidIp(string? value) =>
            !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value.Trim(), out _);

        public static void EnsureValidIp(string value, string fieldName)
        {
            if (!IsValidIp(value))
                throw new ArgumentException($"{fieldName}: '{value}' не является корректным IP-адресом.");
        }

        /// <summary>Префикс подсети для отчётов (например "192.168.1") — набор октетов.</summary>
        public static bool IsValidIpPrefix(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var parts = value.Trim().Split('.');
            if (parts.Length is < 1 or > 4) return false;
            return parts.All(p => p.Length is >= 1 and <= 3 && int.TryParse(p, out var n) && n >= 0 && n <= 255);
        }

        public static void EnsureValidIpPrefix(string value, string fieldName)
        {
            if (!IsValidIpPrefix(value))
                throw new ArgumentException($"{fieldName}: '{value}' не является корректным префиксом IP-адреса.");
        }

        public static bool IsValidMac(string? value) =>
            !string.IsNullOrWhiteSpace(value) && MacRegex.IsMatch(value.Trim());

        public static void EnsureValidMac(string value, string fieldName)
        {
            if (!IsValidMac(value))
                throw new ArgumentException($"{fieldName}: '{value}' не является корректным MAC-адресом (формат XX:XX:XX:XX:XX:XX).");
        }

        /// <summary>Дата не из будущего (правило: все DateTime ≤ DateTime.Now).</summary>
        public static void EnsureNotFuture(DateTime value, string fieldName)
        {
            if (value > DateTime.Now)
                throw new ArgumentException($"{fieldName}: дата не может быть из будущего.");
        }

        /// <summary>Парсит дату в стандарте ISO 8601 ("2026-07-14T10:32:13Z").</summary>
        public static DateTime ParseIsoDate(string value, string fieldName)
        {
            if (!DateTime.TryParse(value, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var parsed))
                throw new ArgumentException($"{fieldName}: ожидаемый формат даты ISO 8601, например 2026-07-14T10:32:13Z.");
            return parsed.ToLocalTime();
        }

        /// <summary>Форматирует дату для передачи на сервер в ISO 8601 (UTC, суффикс Z).</summary>
        public static string ToIsoUtc(DateTime value) => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

        /// <summary>FileHash: ровно 64 символа (SHA-256 hex), без пробелов.</summary>
        public static void EnsureFileHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(char.IsWhiteSpace))
                throw new ArgumentException("FileHash: ожидается SHA-256 хеш — 64 символа без пробелов.");
            if (!Regex.IsMatch(value, "^[0-9a-fA-F]{64}$"))
                throw new ArgumentException("FileHash: хеш должен содержать только hex-символы [0-9a-f].");
        }

        /// <summary>Маска подсети: диапазон [16, 32] (только подсети класса B и меньше).</summary>
        public static void EnsureMaskRange(int mask)
        {
            if (mask < 16 || mask > 32)
                throw new ArgumentException("mask: допустимый диапазон [16, 32].");
        }
    }
}

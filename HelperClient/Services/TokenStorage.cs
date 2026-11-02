using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HelperClient.Services
{
    /// <summary>
    /// Хранилище токена авторизации на клиенте.
    /// Токен сохраняется в файле %LocalAppData%/HelperClient/token.json
    /// и подставляется в заголовок "Authorization: Bearer &lt;токен&gt;".
    /// </summary>
    public class TokenStorage
    {
        private static readonly string StorageDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HelperClient");
        private static readonly string StorageFile = Path.Combine(StorageDir, "token.json");

        private class StoredToken
        {
            public string Token { get; set; } = "";
            public DateTime ExpiresAtUtc { get; set; }
        }

        /// <summary>Текущий токен или null, если пользователь не авторизован.</summary>
        public string? Token { get; private set; }

        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        public TokenStorage()
        {
            TryLoad();
        }

        public void Save(string token, TimeSpan lifetime)
        {
            Token = token;
            try
            {
                Directory.CreateDirectory(StorageDir);
                var record = new StoredToken
                {
                    Token = Protect(token),
                    ExpiresAtUtc = DateTime.UtcNow.Add(lifetime)
                };
                File.WriteAllText(StorageFile, JsonSerializer.Serialize(record));
            }
            catch
            {
                // токен остаётся только в памяти процесса
            }
        }

        public void Clear()
        {
            Token = null;
            try
            {
                if (File.Exists(StorageFile))
                    File.Delete(StorageFile);
            }
            catch { /* ignore */ }
        }

        private void TryLoad()
        {
            try
            {
                if (!File.Exists(StorageFile)) return;
                var record = JsonSerializer.Deserialize<StoredToken>(File.ReadAllText(StorageFile));
                if (record is null || string.IsNullOrEmpty(record.Token)) return;

                // если срок явно указан и истёк — не восстанавливаем (сервер вернёт 401)
                if (record.ExpiresAtUtc != default && record.ExpiresAtUtc < DateTime.UtcNow)
                {
                    Clear();
                    return;
                }

                Token = Unprotect(record.Token);
            }
            catch
            {
                Token = null;
            }
        }

        // DPAPI-шифрование значений токена (Windows); на других платформах — base64 passthrough.
        private static string Protect(string plain)
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            if (OperatingSystem.IsWindows())
                return Convert.ToBase64String(ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
            return Convert.ToBase64String(bytes);
        }

        private static string Unprotect(string stored)
        {
            var bytes = Convert.FromBase64String(stored);
            if (OperatingSystem.IsWindows())
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser));
            return Encoding.UTF8.GetString(bytes);
        }
    }
}

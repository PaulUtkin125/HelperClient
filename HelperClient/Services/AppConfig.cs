using System;
using System.IO;
using System.Text.Json;

namespace HelperClient.Services
{
    /// <summary>
    /// Настройки клиента, загружаемые из appsettings.json (секция "Api").
    /// </summary>
    public class AppConfig
    {
        private static readonly JsonSerializerOptions MapOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string BaseUrl { get; set; } = "http://localhost:5000";
        public string RoutePrefix { get; set; } = "/HelperAPI";
        public int TimeoutSeconds { get; set; } = 30;
        /// <summary>Лимит размера файла для Version/Add — 100 МБ (см. спецификацию).</summary>
        public int MaxDownloadFileSizeMb { get; set; } = 100;

        public static AppConfig Load(string? path = null)
        {
            try
            {
                path ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (File.Exists(path))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    if (doc.RootElement.TryGetProperty("Api", out var api))
                        return api.Deserialize<AppConfig>(MapOptions) ?? new AppConfig();
                }
            }
            catch
            {
                // при ошибке чтения настроек используем значения по умолчанию
            }

            return new AppConfig();
        }
    }
}

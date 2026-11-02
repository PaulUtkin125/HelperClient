using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelperClient.Services.Api
{
    /// <summary>
    /// Настройки подключения к HelperAPI (файл appsettings.json рядом с исполняемым файлом).
    /// </summary>
    public class ApiSettings
    {
        /// <summary>Базовый адрес сервера, например http://helper-server:8080</summary>
        public string BaseUrl { get; set; } = "http://localhost:5000";

        /// <summary>Префикс API из спецификации.</summary>
        public string ApiPrefix { get; set; } = "/HelperAPI";

        /// <summary>Статус заявки «Новая» при создании обращения (справочник Status).</summary>
        public int DefaultRequestStatusId { get; set; } = 1;

        /// <summary>Тип машины по умолчанию для профиля (справочник MachineType).</summary>
        public int DefaultMachineType { get; set; } = 1;

        /// <summary>Интервал резервного опроса сетевых интерфейсов (секунды).</summary>
        public int NetworkPollSeconds { get; set; } = 60;

        /// <summary>Интервал heartbeat (минуты), эндпоинт не требует авторизации.</summary>
        public int HeartbeatMinutes { get; set; } = 15;

        public Uri BuildUri(string route) => new(new Uri(BaseUrl.TrimEnd('/') + ApiPrefix), route);
    }

    /// <summary>
    /// Описание ошибки в формате RFC 7807 (Problem Details), который возвращает сервер.
    /// </summary>
    public sealed class ProblemDetailsDto
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("status")] public int Status { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("detail")] public JsonElement? Detail { get; set; }
        [JsonPropertyName("traceId")] public string? TraceId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("errors")] public JsonElement? Errors { get; set; }
    }

    /// <summary>
    /// Исключение сетевого слоя клиента: содержит расшифровку ответа сервера.
    /// </summary>
    public sealed class ApiException : Exception
    {
        public int StatusCode { get; }
        public string? ErrorCode { get; }
        public ProblemDetailsDto? Problem { get; }

        public ApiException(int statusCode, string? code, string message, ProblemDetailsDto? problem = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = code;
            Problem = problem;
        }

        /// <summary>Конфликт версий (оптимистичный контроль параллелизма).</summary>
        public bool IsConcurrencyConflict =>
            StatusCode == 409 && (ErrorCode == "VERSION_CONFLICT" || ErrorCode is null);

        /// <summary>Запись не найдена на сервере.</summary>
        public bool IsNotFound => StatusCode == 404;

        /// <summary>Сервер недоступен / таймаут.</summary>
        public bool IsNetworkFailure => StatusCode == 0;
    }

    internal static class JsonOptionsProvider
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}

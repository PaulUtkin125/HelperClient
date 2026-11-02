using System;
using HelperClient.Models.DTO.Common;

namespace HelperClient.Services
{
    /// <summary>
    /// Исключение, выбрасываемое при получении неуспешного ответа от /HelperAPI.
    /// Сохраняет HTTP-код и тело ошибки в формате RFC 7807 (ProblemDetailsDto).
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>HTTP-код ответа (400, 401, 404, 409, 413, 422).</summary>
        public int StatusCode { get; }

        /// <summary>Разобранная ошибка в формате Problem Details.</summary>
        public ProblemDetailsDto? Problem { get; }

        /// <summary>Машинно-читаемый код ошибки (VERSION_CONFLICT, FOREIGN_KEY_VIOLATION, ...).</summary>
        public string? ErrorCode => Problem?.Code;

        public ApiException(int statusCode, ProblemDetailsDto? problem)
            : base(BuildMessage(statusCode, problem))
        {
            StatusCode = statusCode;
            Problem = problem;
        }

        private static string BuildMessage(int statusCode, ProblemDetailsDto? problem)
        {
            var message = problem?.Message;
            return string.IsNullOrWhiteSpace(message)
                ? $"Ошибка запроса к API (HTTP {statusCode})."
                : $"HTTP {statusCode} [{problem.Code}]: {message}";
        }

        /// <summary>Конфликт оптимистичной блокировки (ConcurrencyStamp устарел).</summary>
        public bool IsConcurrencyConflict => StatusCode == 409 && ErrorCode == "VERSION_CONFLICT";

        /// <summary>Нарушение уникальности (дубликат записи).</summary>
        public bool IsDuplicate => StatusCode == 409 && !IsConcurrencyConflict;

        /// <summary>Запись используется в других таблицах (FK violation).</summary>
        public bool IsForeignKeyViolation => StatusCode == 400 && ErrorCode == "FOREIGN_KEY_VIOLATION";

        /// <summary>Токен истёк или отсутствует.</summary>
        public bool IsUnauthorized => StatusCode == 401;
    }
}

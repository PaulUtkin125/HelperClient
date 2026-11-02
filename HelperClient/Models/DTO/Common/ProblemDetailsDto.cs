using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelperClient.Models.DTO.Common
{
    /// <summary>
    /// Формат ошибок API — RFC 7807 (Problem Details).
    /// Пример: type=https://httpstatuses.io/409, code=VERSION_CONFLICT.
    /// </summary>
    public class ProblemDetailsDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        /// <summary>Машинно-читаемый код ошибки (например VERSION_CONFLICT, FOREIGN_KEY_VIOLATION).</summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Детали ошибки. Для FOREIGN_KEY_VIOLATION содержит список заблокированных записей,
        /// в остальных случаях может быть строкой или null.
        /// </summary>
        [JsonPropertyName("detail")]
        public JsonElement? Detail { get; set; }

        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        /// <summary>Разбирает detail как список нарушений внешних ключей.</summary>
        public List<ForeignKeyViolationDto>? GetForeignKeyViolations()
        {
            if (Detail is null || Code != "FOREIGN_KEY_VIOLATION")
                return null;

            try
            {
                return Detail.Value.Deserialize<List<ForeignKeyViolationDto>>();
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Запись, которую нельзя удалить/изменить из-за связей.</summary>
    public class ForeignKeyViolationDto
    {
        [JsonPropertyName("entity")]
        public string? Entity { get; set; }

        [JsonPropertyName("entityId")]
        public int EntityId { get; set; }

        [JsonPropertyName("blockedBy")]
        public List<BlockedByDto> BlockedBy { get; set; } = new();
    }

    public class BlockedByDto
    {
        [JsonPropertyName("table")]
        public string? Table { get; set; }

        [JsonPropertyName("field")]
        public string? Field { get; set; }
    }
}
